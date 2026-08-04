using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;

namespace RoomClient.Helpers
{
    public sealed class WebViewPlayer : IDisposable
    {
        private readonly WebView2 _webView;
        private bool _disposed;

        public WebViewPlayer(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));

            if (_webView.CoreWebView2 is null)
            {
                throw new InvalidOperationException("WebView2 must be initialized before creating WebViewPlayer.");
            }

            _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            // Lepas handler lama jika ada untuk mencegah duplikasi event
            _webView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
            _webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;

            // Tambahkan filter dengan aman (cegah error jika filter sudah terdaftar)
            try
            {
                _webView.CoreWebView2.AddWebResourceRequestedFilter(
                    "*",
                    CoreWebView2WebResourceContext.All);
            }
            catch
            {
                // Filter sudah ada pada instance CoreWebView2 ini
            }

            _webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }

        public void LoadHtml(string html)
        {
            ThrowIfDisposed();

            if (_webView.CoreWebView2 is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                Clear();
                return;
            }

            _webView.NavigateToString(html);
        }

        public void Clear()
        {
            if (_disposed || _webView.CoreWebView2 is null)
            {
                return;
            }

            try
            {
                // Navigasi ke about:blank untuk memutus seluruh stream audio/video Chromium secara instan
                _webView.CoreWebView2.Navigate("about:blank");
            }
            catch
            {
                // Fallback jika CoreWebView2 sedang dalam transisi
                _webView.NavigateToString("<html><body style='background:black;margin:0'></body></html>");
            }
        }

        private void CoreWebView2_WebResourceRequested(
            object? sender,
            CoreWebView2WebResourceRequestedEventArgs e)
        {
            try
            {
                e.Request.Headers.SetHeader(
                    "Referer",
                    "https://roomclient.local/");
            }
            catch
            {
            }
        }

        private void CoreWebView2_NavigationCompleted(
            object? sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_webView.CoreWebView2 is not null)
            {
                // 1. Hentikan pemutaran lagu lama secara total sebelum objek dibuang
                Clear();

                // 2. Unhook semua event listener
                _webView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                _webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;

                // 3. Bersihkan filter resource request
                try
                {
                    _webView.CoreWebView2.RemoveWebResourceRequestedFilter(
                        "*",
                        CoreWebView2WebResourceContext.All);
                }
                catch
                {
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebViewPlayer));
            }
        }
    }
}