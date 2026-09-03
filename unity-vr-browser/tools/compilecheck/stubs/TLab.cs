// Faithful stand-in for the TLabWebView API, transcribed from the real sources
// at github.com/TLabAltoh/TLabWebView (branch upm). Only what our adapter calls.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TLab.WebView
{
    public enum CaptureMode { HardwareBuffer, ByteBuffer, Surface }

    public class Download
    {
        public enum Directory { AppData, Download }
        public class Option { }
        public class Request { public string url, userAgent, contentDisposition, mimeType; }
        public class EventInfo { public string url; public long id; }
    }

    public class AlertDialog { public class Init { } public enum Result { Ok, Cancel } }

    public struct AsyncString { public int status; public string value; }

    public class EventCallback
    {
        public UnityEvent<string> onPageStart = new UnityEvent<string>();
        public UnityEvent<string> onPageFinish = new UnityEvent<string>();
        public UnityEvent<Download.Request> onDownload = new UnityEvent<Download.Request>();
    }

    public abstract class FragmentCapture : MonoBehaviour
    {
        public enum State { None, Initialising, Initialized, Destroyed }
        protected State m_state = State.None;
        public State state => m_state;
        public RawImage rawImage => null;
        public Vector2Int viewSize => new Vector2Int(1280, 900);
        public Vector2Int texSize => new Vector2Int(1280, 900);
        public int fps => 30;
        public CaptureMode captureMode => CaptureMode.ByteBuffer;
        public virtual void Init() { }
        public virtual void Init(Vector2Int viewSize, Vector2Int texSize) { }
        public void UpdateFrame() { }
        public void Resize(Vector2Int texSize, Vector2Int viewSize) { }
        public void ResizeTex(Vector2Int texSize) { }
        public void ResizeView(Vector2Int viewSize) { }
        public void SetFps(int fps) { }
    }

    public abstract class Browser : FragmentCapture
    {
        public string url => "";
        public Download.Option downloadOption => null;
        public string[] intentFilters => null;
        public EventCallback eventCallback => null;
        public virtual string package => "";
        public void InitOption(string url, int fps, Download.Option downloadOption) { }
        public void Init(Vector2Int viewSize, Vector2Int texSize, string url, int fps, Download.Option downloadOption) { }
        public void EvaluateJS(string js) { }
        public string GetUrl() => "";
        public void LoadUrl(string url) { }
        public void SetIntentFilters(string[] filters) { }
        public string GetAsyncResult(int id) => "";
        public void CancelAsyncResult(int id) { }
        public IEnumerator<AsyncString> GetUserAgent() => null;
        public void SetUserAgent(string ua, bool reload) { }
        public int GetScrollX() => 0;
        public int GetScrollY() => 0;
        public void ScrollTo(int x, int y) { }
        public void ScrollBy(int x, int y) { }
        public void DispatchMessageQueue() { }
        public void GoForward() { }
        public void GoBack() { }
        public long TouchEvent(int x, int y, int action, long downTime) => 0L;
        public void KeyEvent(char key) { }
        public void KeyEvent(int keyCode) { }
        public void DownloadFromUrl(string url, string ua, string cd, string mime) { }
        public void SetDownloadOption(Download.Option o) { }
        public float GetDownloadProgress(long id) => 0f;
        public void PostDialogResult(AlertDialog.Result result, string json = "") { }
    }

    public class WebView : Browser
    {
        public override string package => "com.tlab.webkit.chromium.UnityConnect";
        public void ZoomIn() { }
        public void ZoomOut() { }
        public void PageUp(bool top) { }
        public void PageDown(bool bottom) { }
        public byte[] GetJSBuffer(string id) => new byte[0];
        public void ClearCache(bool includeDiskFiles) { }
        public void ClearCookie() { }
        public void ClearHistory() { }
        public void LoadHTML(string html, string baseURL) { }
    }

    public class GeckoView : Browser
    {
        public override string package => "com.tlab.webkit.gecko.UnityConnect";
        public void LoadHTML(string html) { }
        public void ClearData(int flag) { }
    }
}
