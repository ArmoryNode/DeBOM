# DeBOM

## 💡 About

DeBOM is a small tool for removing BOM (Byte Order Mark) from files. I started this project mainly to learn more about writing idiomatic F# code, railway-oriented programming, and asynchronous workflows.

## 🚀 Usage

> [!NOTE]
> It's recommended to use the `--copy` option to create a backup of the original file(s) the first time to verify that the correct files are processed. 

```ini
Arguments:
  <path>                      A directory or file path
Options:
  --copy, -c                  Stores a copy of the processed files instead of overwriting the original(s). (Default: false)
  --recursive, -r             Search recursively in subdirectories. This option is ignored when a file path is provided. (Default: false)
  --pattern, -p <pattern>     Pattern to match files against. This option is ignored when a file path is provided. (Default: *)
  --help                      Show the help text
```

## 📦 Third-Party Libraries & Licenses

- [ColoredPrintf](https://github.com/vbfox/ColoredPrintf) (MIT License)

- [Argu](https://github.com/fsprojects/Argu) (MIT License)

- [FSharp.Control.FusionTasks](https://github.com/kekyo/FSharp.Control.FusionTasks) (MIT License)

## 📜 License

This project is licensed under the Apache License, Version 2.0 (see [LICENSE](LICENSE)).