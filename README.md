# dotnet WASM BrowserHttpHandler with support for ReadableStream request bodies

This allows the usage of request streaming when supported. In addition to that it also needs to be opted in via:
```c#
request.Options.Set(BrowserHttpHandler.EnableStreamingRequest, true);
```

For streaming requests over HTTP 1 the following setting is also required:
```c#
request.SetBrowserRequestOption("allowHTTP1ForStreamingUpload", true);
```

Currently this feature `Experimental Web Platform features` needs to be enabled in chrome://flags/

## Code

- [Index.razor](Client/Pages/Index.razor): streaming inputs
- [Upload.razor](Client/Pages/Index.razor): upload multipart form file

- [BrowserHttpHandler.cs](Client/BrowserHttpHandler.cs): modified version of the [dotnet/runtime](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/BrowserHttpHandler/BrowserHttpHandler.cs) code

## Resources

- dotnet runtime issue: https://github.com/dotnet/runtime/issues/36634
- Google Chrome feature status: https://www.chromestatus.com/feature/5274139738767360
- ReadableStream MDN: https://developer.mozilla.org/docs/Web/API/ReadableStream/ReadableStream
- web.dev article which the index example is based on: https://web.dev/fetch-upload-streaming/
