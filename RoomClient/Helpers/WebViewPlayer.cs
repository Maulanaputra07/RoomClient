using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

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
            _webView.CoreWebView2.AddWebResourceRequestedFilter(
                "*",
                CoreWebView2WebResourceContext.All);
            _webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }

        public void LoadHtml(string html)
        {
            ThrowIfDisposed();
            _webView.NavigateToString(html);
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
                _webView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                _webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
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