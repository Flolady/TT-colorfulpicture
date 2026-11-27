# 彩图工具（独立兼容实现）

[English](README.en.md) | [日本語](README.ja.md)

这是一个 Windows 桌面小工具，可从 PNG 彩图提取文件，也可将文件封装到 PNG 图片中。多个输入文件会先打包成 ZIP，再封装；提取后可自行解压 ZIP。

## 项目来源

本项目不是 `TT-tools` 的官方版本，也不是把整个 `TT-tools` 仓库直接编译出来的程序。界面和本项目代码是为独立运行编写的兼容实现。

实现时参考了公开仓库 [linyi1111123/TT-tools](https://github.com/linyi1111123/TT-tools) 中旧版无密码彩图处理流程，主要用于确认数据布局与兼容目标：

- [`tt_img_enc_node.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_enc_node.py)：单图/多图输入的文件组织及文件头格式。
- [`tt_img_utils.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_utils.py)：把数据写入 RGB 像素最低位的方式。
- [`tt_img_dec_node.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_dec_node.py)：从像素最低位读取数据的方式。

本项目未复制上述仓库的源码文件。它只针对所参考的旧版、无密码 RGB-LSB 数据格式；不保证兼容其他版本、密码版或不同工具生成的彩图。原仓库主要是 ComfyUI 自定义节点，本项目没有包含 ComfyUI。

## 功能

- 批量提取 PNG 彩图中的嵌入文件。
- 将单个文件封装成 PNG 彩图。
- 将多个文件先打包为 ZIP，再封装成 PNG 彩图。
- 文件列表支持 Ctrl/Shift 多选和批量移除。
- 解码后的扩展名会限制为字母和数字，异常扩展名按 `.bin` 保存。

## 构建

需要 Windows、.NET 8 SDK 和 Windows Desktop Runtime。项目不依赖第三方 NuGet 包。

```powershell
dotnet publish .\ColorImageTool.csproj -c Release --self-contained false -o .\publish
```

生成的程序需要目标电脑安装 .NET 8 Desktop Runtime。

## 格式与限制

- 输入彩图必须为 PNG。JPEG 等有损压缩、缩放或调色可能会损坏嵌入数据。
- 单文件封装会保留原始扩展名和文件字节；多文件输入会生成 `.zip` 数据。
- 该格式主要是像素隐写/封装，不是可靠的机密加密。请勿用它保护敏感资料。
- 本仓库暂不附带开源许可证。

