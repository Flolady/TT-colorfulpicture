# カラー画像ツール（独立互換実装）

[简体中文](README.md) | [English](README.en.md)

PNG の「彩图」画像からファイルを取り出したり、ファイルを PNG 画像に埋め込んだりする Windows 用デスクトップアプリです。複数のファイルを埋め込む場合は、先に ZIP にまとめます。取り出した ZIP は解凍してご利用ください。

## プロジェクトの出典

このプロジェクトは `TT-tools` の公式版ではなく、`TT-tools` リポジトリ全体をそのままビルドしたものでもありません。独立して動作する互換実装として、画面とソースコードを作成しています。

データ形式と互換対象を確認するため、公開されている [linyi1111123/TT-tools](https://github.com/linyi1111123/TT-tools) の旧式・パスワードなしカラー画像処理を参考にしました。

- [`tt_img_enc_node.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_enc_node.py)：単一/複数画像の処理とデータヘッダー形式。
- [`tt_img_utils.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_utils.py)：RGB ピクセルの最下位ビットへのデータ格納。
- [`tt_img_dec_node.py`](https://github.com/linyi1111123/TT-tools/blob/master/tt_img_dec_node.py)：ピクセルの最下位ビットからのデータ抽出。

上記リポジトリのソースファイルは本プロジェクトにコピーしていません。このプログラムが対象とするのは、参照した旧式のパスワードなし RGB-LSB 形式のみです。他のバージョン、パスワード付き形式、または別ツールで作成された画像との互換性は保証されません。元のリポジトリは主に ComfyUI 用カスタムノードであり、本プロジェクトに ComfyUI は含まれません。

## 機能

- PNG カラー画像からの複数ファイル一括抽出。
- 1 つのファイルを PNG 画像に埋め込み。
- 複数ファイルを ZIP にまとめてから PNG 画像に埋め込み。
- ファイル一覧で Ctrl/Shift による複数選択と一括削除。
- 抽出時の拡張子は英数字のみに制限し、不正な拡張子は `.bin` として保存。

## ビルド

Windows、.NET 8 SDK、Windows Desktop Runtime が必要です。サードパーティの NuGet パッケージには依存していません。

```powershell
dotnet publish .\ColorImageTool.csproj -c Release --self-contained false -o .\publish
```

生成したアプリを実行する PC には .NET 8 Desktop Runtime が必要です。

## 形式と制限

- 入力画像は PNG のみです。JPEG などの非可逆圧縮、リサイズ、色調整によって埋め込みデータが破損する場合があります。
- 単一ファイルは元の拡張子とバイト列のまま保存します。複数ファイルは `.zip` のデータとして保存します。
- この形式は主に画像ピクセルへのデータ隠蔽/格納であり、信頼できる暗号化ではありません。機密情報の保護には使用しないでください。
- このリポジトリにはオープンソースライセンスを付属していません。

