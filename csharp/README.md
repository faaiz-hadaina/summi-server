# Summi Server — C# Minimal API

This folder contains a minimal ASP.NET Core (net7.0) implementation of the original Node.js API. It uses an in-memory store for contacts so you can run and test without a database.

How to build and run (macOS):

1. Make sure you have the .NET SDK installed (recommend .NET 7). Check with:

```bash
dotnet --version
```

2. From the `csharp` folder, build and run:

```bash
cd csharp
dotnet build
dotnet run
```

3. The API will listen on the default Kestrel URL (normally `http://localhost:5000` or a different port shown in the console). Endpoints mirror the Node version under `/api`.

Notes

- Static files: the app serves a `files/` directory. If you keep the repository layout unchanged, the server will look for `../files` relative to the `csharp` folder and serve those files at `/files`.
- This implementation intentionally uses an in-memory store to stay simple and easily runnable. To switch to a database (e.g. MongoDB) you can add the MongoDB driver and replace the store logic.
