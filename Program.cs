using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace ColorImageTool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly ListBox files = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, SelectionMode = SelectionMode.MultiExtended };
    private readonly TextBox folder = new() { Dock = DockStyle.Fill };
    private readonly Button add = new() { Text = "添加文件…", AutoSize = true };
    private readonly Button remove = new() { Text = "移除所选", AutoSize = true };
    private readonly Button extract = new() { Text = "提取彩图", AutoSize = true };
    private readonly Button encode = new() { Text = "封装成彩图", AutoSize = true };
    private readonly Button browse = new() { Text = "浏览…", AutoSize = true };
    private readonly Label status = new() { AutoSize = true, Text = "就绪" };

    public MainForm()
    {
        Text = "彩图工具";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(620, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var intro = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = "提取或制作基于 TT-tools 旧版 RGB 最低位格式的彩图。支持 PNG；封装多个文件时会先打成 ZIP。"
        };
        root.Controls.Add(intro, 0, 0);

        var pathRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true, Margin = new Padding(0, 12, 0, 8) };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathRow.Controls.Add(new Label { Text = "保存位置", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
        folder.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        pathRow.Controls.Add(folder, 1, 0);
        pathRow.Controls.Add(browse, 2, 0);
        root.Controls.Add(pathRow, 0, 1);
        root.Controls.Add(files, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 10, 0, 4) };
        buttons.Controls.AddRange([add, remove, extract, encode]);
        root.Controls.Add(buttons, 0, 3);
        root.Controls.Add(status, 0, 4);

        add.Click += (_, _) => AddFiles();
        remove.Click += (_, _) =>
        {
            foreach (var index in files.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToArray())
                files.Items.RemoveAt(index);
        };
        browse.Click += (_, _) => ChooseFolder();
        extract.Click += async (_, _) => await RunExtractAsync();
        encode.Click += async (_, _) => await RunEncodeAsync();
        files.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        files.DragDrop += (_, e) => { if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AddPaths(paths); };
        files.AllowDrop = true;
    }

    private void AddFiles()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = "选择文件" };
        if (dialog.ShowDialog(this) == DialogResult.OK) AddPaths(dialog.FileNames);
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        var known = files.Items.Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
            if (File.Exists(path) && known.Add(path)) files.Items.Add(path);
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(folder.Text) ? folder.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) };
        if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
    }

    private string[] SelectedPaths() => files.Items.Cast<string>().ToArray();

    private async Task RunExtractAsync()
    {
        var paths = SelectedPaths();
        if (paths.Length == 0) { MessageBox.Show(this, "先添加一张或多张彩图 PNG。", "提示"); return; }
        var dest = folder.Text.Trim();
        SetBusy(true, "正在提取…");
        try
        {
            Directory.CreateDirectory(dest);
            var result = await Task.Run(() =>
            {
                var saved = new List<string>();
                foreach (var path in paths)
                {
                    if (!Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"只支持 PNG 彩图：{Path.GetFileName(path)}");
                    var (data, extension) = StegoCodec.Decode(path);
                    var name = Path.GetFileNameWithoutExtension(path) + "_提取" + NormalizeExtension(extension);
                    var output = UniquePath(dest, name);
                    File.WriteAllBytes(output, data);
                    saved.Add(output);
                }
                return saved;
            });
            SetBusy(false, $"提取完成：{result.Count} 个文件已保存到所选目录。");
            MessageBox.Show(this, string.Join(Environment.NewLine, result), "提取完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { SetBusy(false, "提取失败。"); MessageBox.Show(this, ex.Message, "提取失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task RunEncodeAsync()
    {
        var paths = SelectedPaths();
        if (paths.Length == 0) { MessageBox.Show(this, "先添加要封装的文件。", "提示"); return; }
        var dest = folder.Text.Trim();
        SetBusy(true, "正在封装…");
        try
        {
            Directory.CreateDirectory(dest);
            var output = await Task.Run(() =>
            {
                byte[] payload;
                string extension;
                if (paths.Length == 1)
                {
                    payload = File.ReadAllBytes(paths[0]);
                    extension = Path.GetExtension(paths[0]);
                }
                else
                {
                    using var memory = new MemoryStream();
                    using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
                    {
                        foreach (var path in paths)
                        {
                            var entry = zip.CreateEntry(Path.GetFileName(path), CompressionLevel.Optimal);
                            using var target = entry.Open();
                            using var source = File.OpenRead(path);
                            source.CopyTo(target);
                        }
                    }
                    payload = memory.ToArray();
                    extension = ".zip";
                }
                var output = UniquePath(dest, "彩图_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                StegoCodec.Encode(payload, extension, output);
                return output;
            });
            SetBusy(false, "封装完成。");
            MessageBox.Show(this, output, "彩图已生成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { SetBusy(false, "封装失败。"); MessageBox.Show(this, ex.Message, "封装失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static string NormalizeExtension(string extension)
    {
        extension = extension.Trim();
        if (extension.Length == 0) return ".bin";
        extension = extension.TrimStart('.');
        if (extension.Length is 0 or > 16 || extension.Any(c => !char.IsAsciiLetterOrDigit(c)))
            return ".bin";
        return "." + extension;
    }

    private static string UniquePath(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path)) return path;
        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (var i = 1; ; i++)
        {
            path = Path.Combine(directory, $"{stem}_{i}{ext}");
            if (!File.Exists(path)) return path;
        }
    }

    private void SetBusy(bool busy, string message)
    {
        add.Enabled = remove.Enabled = extract.Enabled = encode.Enabled = browse.Enabled = !busy;
        status.Text = message;
        UseWaitCursor = busy;
    }
}

internal static class StegoCodec
{
    private const int SkipTopPercent = 5;

    public static (byte[] Data, string Extension) Decode(string path)
    {
        using var bitmap = new Bitmap(path);
        var (pixels, stride, width, height) = ReadRgb(bitmap);
        try
        {
            var yStart = height * SkipTopPercent / 100;
            var bits = new BitReader(pixels, stride, width, height, yStart);
            uint headerLength = 0;
            for (var i = 0; i < 32; i++) headerLength = (headerLength << 1) | (uint)bits.ReadBit();
            var capacityBits = (long)(height - yStart) * width * 3;
            if (headerLength < 5 || headerLength > (capacityBits - 32) / 8 || headerLength > int.MaxValue)
                throw new InvalidDataException("没有找到有效的彩图数据，或图像像素已被压缩/改动。请使用原始 PNG。");
            var header = new byte[(int)headerLength];
            for (var i = 0; i < header.Length; i++)
            {
                byte value = 0;
                for (var b = 0; b < 8; b++) value = (byte)((value << 1) | bits.ReadBit());
                header[i] = value;
            }
            var extLength = header[0];
            if (header.Length < 1 + extLength + 4) throw new InvalidDataException("彩图数据头损坏。");
            var extension = new UTF8Encoding(false, true).GetString(header, 1, extLength);
            var size = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(1 + extLength, 4));
            var start = 1 + extLength + 4;
            if (size > header.Length - start) throw new InvalidDataException("彩图文件内容不完整。");
            return (header.AsSpan(start, (int)size).ToArray(), extension);
        }
        finally { Marshal.FreeHGlobal(pixels); }
    }

    public static void Encode(byte[] payload, string extension, string path)
    {
        extension = extension.TrimStart('.');
        var extBytes = Encoding.UTF8.GetBytes(extension);
        if (extBytes.Length > 255) throw new InvalidDataException("扩展名过长。");
        if (payload.LongLength > uint.MaxValue) throw new InvalidDataException("单个文件不能超过 4 GB。");
        using var headerStream = new MemoryStream();
        headerStream.WriteByte((byte)extBytes.Length);
        headerStream.Write(extBytes);
        Span<byte> len = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(len, (uint)payload.Length);
        headerStream.Write(len);
        headerStream.Write(payload);
        var header = headerStream.ToArray();

        var bitsNeeded = 32L + (long)header.Length * 8;
        var yPercentSkip = SkipTopPercent / 100.0;
        var side = Math.Max(64, (int)Math.Ceiling(Math.Sqrt(bitsNeeded / (3.0 * (1.0 - yPercentSkip)))));
        var top = side * SkipTopPercent / 100;
        while ((long)(side - top) * side * 3 < bitsNeeded) { side++; top = side * SkipTopPercent / 100; }
        using var bitmap = new Bitmap(side, side, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, side, side);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            var buffer = new byte[data.Stride * side];
            Array.Fill(buffer, (byte)128);
            var sourceBit = 0L;
            var totalDataBits = bitsNeeded;
            for (var y = top; y < side && sourceBit < totalDataBits; y++)
            {
                for (var x = 0; x < side && sourceBit < totalDataBits; x++)
                {
                    var p = y * data.Stride + x * 3;
                    // The legacy encoder traverses RGB. All three channels carry one bit.
                    for (var channel = 2; channel >= 0 && sourceBit < totalDataBits; channel--)
                    {
                        var bit = sourceBit < 32
                            ? (int)((uint)header.Length >> (31 - (int)sourceBit) & 1)
                            : (header[(int)((sourceBit - 32) / 8)] >> (7 - (int)((sourceBit - 32) % 8))) & 1;
                        buffer[p + channel] = (byte)((buffer[p + channel] & 0xFE) | bit);
                        sourceBit++;
                    }
                }
            }
            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
        }
        finally { bitmap.UnlockBits(data); }
        bitmap.Save(path, ImageFormat.Png);
    }

    private static (IntPtr Pixels, int Stride, int Width, int Height) ReadRgb(Bitmap source)
    {
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = stride * data.Height;
            var managed = new byte[bytes];
            Marshal.Copy(data.Scan0, managed, 0, bytes);
            var copy = Marshal.AllocHGlobal(bytes);
            Marshal.Copy(managed, 0, copy, bytes);
            return (copy, stride, source.Width, source.Height);
        }
        finally { source.UnlockBits(data); }
    }

    private sealed class BitReader(IntPtr pixelData, int stride, int width, int height, int yStart)
    {
        private long index;
        private readonly long available = (long)(height - yStart) * width * 3;
        public int ReadBit()
        {
            if (index >= available) throw new InvalidDataException("彩图像素数据不足。");
            var pixel = index / 3;
            var channel = (int)(index % 3);
            var y = yStart + pixel / width;
            var x = pixel % width;
            // Source stores RGB order; 24bpp GDI+ memory is BGR order.
            var memoryChannel = channel switch { 0 => 2, 1 => 1, _ => 0 };
            var value = Marshal.ReadByte(pixelData, checked((int)(y * stride + x * 4 + memoryChannel)));
            index++;
            return value & 1;
        }
    }
}

