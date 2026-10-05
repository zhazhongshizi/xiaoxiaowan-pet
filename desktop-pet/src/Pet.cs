using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("小小湾桌宠")]
[assembly: AssemblyProduct("小小湾桌宠")]
[assembly: AssemblyDescription("Offline animated desktop companion")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace XiaoXiaoWan
{
    [DataContract]
    public sealed class Preferences
    {
        [DataMember] public int Version = 1;
        [DataMember] public double Scale = 1.5;
        [DataMember] public bool AlwaysOnTop = true;
        [DataMember] public bool FollowCursor = true;
        [DataMember] public bool Wander = true;
        [DataMember] public bool HasPosition;
        [DataMember] public int X;
        [DataMember] public int Y;
    }
    internal static class Geometry
    {
        internal static Point Clamp(Point p, Size size, Rectangle area)
        {
            return new Point(Math.Max(area.Left, Math.Min(p.X, area.Right - size.Width)),
                Math.Max(area.Top, Math.Min(p.Y, area.Bottom - size.Height)));
        }
        internal static int Direction(Point cursor, Point center)
        {
            double a = Math.Atan2(cursor.X - center.X, center.Y - cursor.Y) * 180 / Math.PI;
            return ((int)Math.Round((a + 360) / 22.5)) % 16;
        }
    }
    internal sealed class Sprites : IDisposable
    {
        internal static readonly int[][] Durations = {
            new [] {280,110,110,140,140,320},
            new [] {120,120,120,120,120,120,120,120},
            new [] {120,120,120,120,120,120,120,120},
            new [] {140,140,140,280}, new [] {140,140,140,140,280},
            new [] {140,140,140,140,140,140,140,240},
            new [] {150,150,150,150,150,260}, new [] {120,120,120,120,120,220},
            new [] {150,150,150,150,150,280},
            new [] {160,160,160,160,160,160,160,160}, new [] {160,160,160,160,160,160,160,160}
        };
        private readonly Bitmap source;
        private readonly Dictionary<int, Bitmap> cache = new Dictionary<int, Bitmap>();
        internal Size Size { get; private set; }
        internal Sprites()
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("atlas.png"))
            using (Bitmap image = new Bitmap(s)) source = new Bitmap(image);
            if (source.Width != 1536 || source.Height != 2288) throw new InvalidDataException("Invalid sprite dimensions.");
            Resize(1.5);
        }
        internal void Resize(double scale)
        {
            foreach (Bitmap b in cache.Values) b.Dispose();
            cache.Clear();
            Size = new Size((int)Math.Round(192 * scale), (int)Math.Round(208 * scale));
        }
        internal Bitmap Get(int row, int column)
        {
            int key = row * 8 + column;
            Bitmap frame;
            if (!cache.TryGetValue(key, out frame))
            {
                frame = new Bitmap(Size.Width, Size.Height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(frame))
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(source, new Rectangle(Point.Empty, Size), column * 192, row * 208,
                        192, 208, GraphicsUnit.Pixel, attributes);
                }
                cache.Add(key, frame);
            }
            return frame;
        }
        public void Dispose()
        {
            foreach (Bitmap b in cache.Values) b.Dispose();
            source.Dispose();
        }
    }
    internal static class Native
    {
        internal const int HTTRANSPARENT = -1, HTCLIENT = 1, WM_NCHITTEST = 0x84;
        internal static readonly uint ShowMessage = RegisterWindowMessage("XiaoXiaoWan.Show.1");
        [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] internal struct SIZE { public int X, Y; public SIZE(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct BLEND { public byte Op, Flags, Alpha, Format; }
        [DllImport("user32.dll")] internal static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window,
            IntPtr destDC, ref POINT dest, ref SIZE size, IntPtr srcDC, ref POINT source,
            int color, ref BLEND blend, int flags);
        internal static void Paint(IntPtr window, Point location, Bitmap frame)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, bitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen);
                bitmap = frame.GetHbitmap(Color.FromArgb(0));
                old = SelectObject(memory, bitmap);
                POINT dest = new POINT(location.X, location.Y), src = new POINT(0, 0);
                SIZE size = new SIZE(frame.Width, frame.Height);
                BLEND blend = new BLEND { Op = 0, Alpha = 255, Format = 1 };
                if (!UpdateLayeredWindow(window, screen, ref dest, ref size, memory, ref src, 0, ref blend, 2))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (old != IntPtr.Zero) SelectObject(memory, old);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
    internal sealed class Pet : Form
    {
        private readonly Sprites sprites = new Sprites();
        private readonly Preferences prefs;
        private readonly string settingsPath;
        private readonly bool verification;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Random random = new Random();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 33 };
        private readonly NotifyIcon tray;
        private readonly Icon icon;
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly ToolStripMenuItem pauseItem;
        private readonly List<ToolStripMenuItem> sizeItems = new List<ToolStripMenuItem>();
        private int row, frame, paintedKey = -1;
        private double previousTime, frameTime, lastCursorChange, nextActivity, walkRemaining, walkX;
        private int walkDirection;
        private bool oneShot, paused, dragging, moved, doubleClicked, shuttingDown;
        private Point dragCursor, dragLocation, previousCursor;
        private Bitmap currentFrame;
        internal bool VerificationFailed { get; private set; }
        internal Pet(bool test)
        {
            verification = test;
            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XiaoXiaoWan", "settings.json");
            prefs = test ? new Preferences { Wander = false } : LoadPreferences();
            Text = "小小湾";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = prefs.AlwaysOnTop;
            sprites.Resize(prefs.Scale);
            Size = sprites.Size;
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = prefs.HasPosition ? Geometry.Clamp(new Point(prefs.X, prefs.Y), Size,
                Screen.FromPoint(new Point(prefs.X + Width / 2, prefs.Y + Height / 2)).WorkingArea)
                : new Point(area.Right - Width - 60, area.Bottom - Height);
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("pet.ico")) icon = new Icon(s);
            Icon = icon;
            menu.Items.Add("小小湾", null, delegate { Play(3); });
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem actions = new ToolStripMenuItem("动作");
            string[] names = {"待机", "向右走", "向左走", "挥手", "跳跃", "沮丧", "等待", "专注", "检查"};
            for (int i = 0; i < names.Length; i++)
            {
                int selected = i;
                actions.DropDownItems.Add(names[i], null, delegate { Play(selected); });
            }
            menu.Items.Add(actions);
            ToolStripMenuItem sizes = new ToolStripMenuItem("大小");
            double[] scales = {1, 1.5, 2, 2.5};
            string[] labels = {"小", "中", "大", "特大"};
            for (int i = 0; i < scales.Length; i++)
            {
                double scale = scales[i];
                ToolStripMenuItem item = new ToolStripMenuItem(labels[i]) { Tag = scale };
                item.Click += delegate { ChangeSize(scale); };
                sizeItems.Add(item);
                sizes.DropDownItems.Add(item);
            }
            menu.Items.Add(sizes);
            AddToggle("始终置顶", prefs.AlwaysOnTop, delegate(bool value) { prefs.AlwaysOnTop = value; TopMost = value; });
            AddToggle("视线跟随", prefs.FollowCursor, delegate(bool value) { prefs.FollowCursor = value; StopWalk(); });
            AddToggle("自由散步", prefs.Wander, delegate(bool value) { prefs.Wander = value; StopWalk(); nextActivity = clock.ElapsedMilliseconds + 3000; });
            pauseItem = AddToggle("暂停", false, delegate(bool value) { paused = value; });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("回到屏幕右下角", null, delegate { ResetPosition(); });
            menu.Items.Add("隐藏", null, delegate { HidePet(); });
            menu.Items.Add("退出", null, delegate { Close(); });
            menu.Opening += delegate { foreach (ToolStripMenuItem item in sizeItems) item.Checked = (double)item.Tag == prefs.Scale; };
            tray = new NotifyIcon { Icon = icon, Text = "小小湾", ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { if (Visible) HidePet(); else RestorePet(); } };
            timer.Tick += Tick;
            MouseDown += OnPetDown;
            MouseMove += OnPetMove;
            MouseUp += OnPetUp;
            MouseDoubleClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) doubleClicked = true; };
            Shown += delegate { previousCursor = Cursor.Position; nextActivity = clock.ElapsedMilliseconds + 8000; Draw(true); timer.Start(); };
            FormClosing += delegate { shuttingDown = true; SavePreferences(); };
        }
        protected override CreateParams CreateParams
        {
            get { CreateParams p = base.CreateParams; p.ExStyle |= 0x80000 | 0x80 | 0x08000000; return p; }
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnPaint(PaintEventArgs e) { }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.ShowMessage) { RestorePet(); return; }
            if (m.Msg == Native.WM_NCHITTEST)
            {
                long value = m.LParam.ToInt64();
                Point p = PointToClient(new Point((short)(value & 0xffff), (short)((value >> 16) & 0xffff)));
                bool hit = currentFrame != null && p.X >= 0 && p.Y >= 0 && p.X < currentFrame.Width && p.Y < currentFrame.Height
                    && currentFrame.GetPixel(p.X, p.Y).A > 20;
                m.Result = new IntPtr(hit ? Native.HTCLIENT : Native.HTTRANSPARENT);
                return;
            }
            if (m.Msg == 0x007e || m.Msg == 0x02e0)
            {
                base.WndProc(ref m);
                if (IsHandleCreated && !shuttingDown) { Location = Geometry.Clamp(Location, Size, Screen.FromRectangle(Bounds).WorkingArea); Draw(true); }
                return;
            }
            base.WndProc(ref m);
        }
        private ToolStripMenuItem AddToggle(string name, bool initial, Action<bool> action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(name) { CheckOnClick = true, Checked = initial };
            item.Click += delegate { action(item.Checked); SavePreferences(); };
            menu.Items.Add(item);
            return item;
        }
        private Preferences LoadPreferences()
        {
            try
            {
                using (FileStream s = File.OpenRead(settingsPath))
                {
                    Preferences result = (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(s);
                    if (result == null || result.Version != 1 || Array.IndexOf(new[] {1.0, 1.5, 2.0, 2.5}, result.Scale) < 0) return new Preferences();
                    return result;
                }
            }
            catch (IOException) { return new Preferences(); }
            catch (UnauthorizedAccessException) { return new Preferences(); }
            catch (SerializationException) { return new Preferences(); }
        }
        private void SavePreferences()
        {
            if (verification) return;
            prefs.X = Left; prefs.Y = Top; prefs.HasPosition = true;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                string temp = settingsPath + ".tmp";
                using (FileStream s = File.Create(temp)) new DataContractJsonSerializer(typeof(Preferences)).WriteObject(s, prefs);
                if (File.Exists(settingsPath)) File.Replace(temp, settingsPath, null); else File.Move(temp, settingsPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        private void ChangeSize(double scale)
        {
            Point feet = new Point(Left + Width / 2, Bottom);
            prefs.Scale = scale;
            currentFrame = null;
            sprites.Resize(scale);
            Size = sprites.Size;
            Location = Geometry.Clamp(new Point(feet.X - Width / 2, feet.Y - Height), Size, Screen.FromPoint(feet).WorkingArea);
            Draw(true); SavePreferences();
        }
        private void ResetPosition()
        {
            StopWalk();
            Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
            Location = Geometry.Clamp(new Point(area.Right - Width - 60, area.Bottom - Height), Size, area);
            Draw(true); SavePreferences();
        }
        private void HidePet() { timer.Stop(); Hide(); StopWalk(); }
        private void RestorePet()
        {
            if (shuttingDown) return;
            Location = Geometry.Clamp(Location, Size, Screen.FromRectangle(Bounds).WorkingArea);
            Show(); previousTime = clock.ElapsedMilliseconds; Draw(true); timer.Start();
        }
        private void OnPetDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true; moved = false; doubleClicked = false; dragCursor = Cursor.Position; dragLocation = Location; Capture = true; StopWalk();
            }
        }
        private void OnPetMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point p = Cursor.Position;
            int dx = p.X - dragCursor.X, dy = p.Y - dragCursor.Y;
            if (!moved && Math.Abs(dx) + Math.Abs(dy) < 5) return;
            moved = true;
            Location = new Point(dragLocation.X + dx, dragLocation.Y + dy);
            Draw(false);
        }
        private void OnPetUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragging)
            {
                dragging = false; Capture = false;
                Location = Geometry.Clamp(Location, Size, Screen.FromRectangle(Bounds).WorkingArea);
                Draw(false); SavePreferences();
                if (!moved) Play(doubleClicked ? 4 : 3);
                nextActivity = clock.ElapsedMilliseconds + 8000;
            }
            if (e.Button == MouseButtons.Right) menu.Show(Cursor.Position);
        }
        private void SetState(int state, bool single)
        {
            row = state; frame = 0; frameTime = 0; oneShot = single;
        }
        private void Play(int state)
        {
            paused = false; pauseItem.Checked = false; StopWalk();
            SetState(state, state != 0);
            if (state == 1 || state == 2) StartWalk(state == 1 ? 1 : -1, 140);
            nextActivity = clock.ElapsedMilliseconds + 8000;
            if (!Visible) RestorePet();
            Draw(true);
        }
        private void StartWalk(int direction, int distance)
        {
            walkDirection = direction; walkX = Left;
            walkRemaining = distance;
            SetState(direction > 0 ? 1 : 2, false);
        }
        private void StopWalk() { walkDirection = 0; SetState(0, false); }
        private void Tick(object sender, EventArgs e)
        {
            double now = clock.ElapsedMilliseconds, dt = Math.Min(100, now - previousTime);
            previousTime = now;
            if (verification && now > 120000) { Close(); return; }
            if (paused || dragging || menu.Visible) return;
            Point cursor = Cursor.Position;
            if (cursor != previousCursor) { previousCursor = cursor; lastCursorChange = now; }
            if (walkDirection != 0)
            {
                Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
                double travel = Math.Min(walkRemaining, 65 * prefs.Scale * dt / 1000);
                walkX += walkDirection * travel;
                walkRemaining -= travel;
                Point desired = new Point((int)Math.Round(walkX), Top);
                Location = Geometry.Clamp(desired, Size, area);
                if (walkRemaining <= 0 || desired.X != Left) { StopWalk(); nextActivity = now + random.Next(6000, 12000); }
            }
            if (walkDirection == 0 && !oneShot && now >= nextActivity)
            {
                nextActivity = now + random.Next(8000, 16000);
                if (prefs.Wander && random.Next(3) != 0) StartWalk(random.Next(2) == 0 ? -1 : 1, random.Next(100, 220));
                else SetState(random.Next(2) == 0 ? 3 : 4, true);
            }
            bool gaze = walkDirection == 0 && !oneShot && prefs.FollowCursor && now - lastCursorChange < 1600;
            Point center = new Point(Left + Width / 2, Top + (int)(Height * 0.27));
            if (gaze && Math.Abs(cursor.X - center.X) + Math.Abs(cursor.Y - center.Y) < 1200)
            {
                int direction = Geometry.Direction(cursor, center);
                row = 9 + direction / 8; frame = direction % 8; frameTime = 0;
            }
            else
            {
                if (row >= 9) SetState(0, false);
                frameTime += dt;
                while (frameTime >= Sprites.Durations[row][frame])
                {
                    frameTime -= Sprites.Durations[row][frame];
                    frame++;
                    if (frame >= Sprites.Durations[row].Length)
                    {
                        if (oneShot) { SetState(0, false); break; }
                        frame = 0;
                    }
                }
            }
            Draw(false);
        }
        private Point paintedLocation = new Point(int.MinValue, int.MinValue);
        private void Draw(bool force)
        {
            if (!IsHandleCreated || !Visible || shuttingDown) return;
            int key = row * 8 + frame;
            if (!force && key == paintedKey && Location == paintedLocation) return;
            currentFrame = sprites.Get(row, frame);
            Native.Paint(Handle, Location, currentFrame);
            paintedKey = key; paintedLocation = Location;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop(); timer.Dispose();
                tray.Visible = false; tray.Dispose(); menu.Dispose(); sprites.Dispose(); icon.Dispose();
            }
            base.Dispose(disposing);
        }
        internal void Verify(string directory)
        {
            Directory.CreateDirectory(directory);
            System.Windows.Forms.Timer verifier = new System.Windows.Forms.Timer { Interval = 400 };
            List<string> results = new List<string>();
            Point savedCursor = Cursor.Position;
            Form backdrop = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual, BackColor = Color.White, TopMost = true, Size = new Size(900, 650) };
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            backdrop.Location = new Point(area.Left + (area.Width - backdrop.Width) / 2, area.Top + (area.Height - backdrop.Height) / 2);
            int stage = 0;
            Point start = Point.Empty;
            Action finish = delegate {
                verifier.Stop(); verifier.Dispose();
                Cursor.Position = savedCursor;
                backdrop.Close(); backdrop.Dispose();
                File.WriteAllLines(Path.Combine(directory, "native-window-test.txt"), results.ToArray());
                Close();
            };
            verifier.Tick += delegate {
                try
                {
                    switch (stage++)
                    {
                        case 0:
                            prefs.FollowCursor = false; prefs.Wander = false; StopWalk();
                            nextActivity = clock.ElapsedMilliseconds + 60000;
                            backdrop.Show();
                            Location = new Point(backdrop.Left + 240, backdrop.Top + 170);
                            TopMost = false; TopMost = true; Draw(true);
                            break;
                        case 1:
                            using (Bitmap capture = new Bitmap(Width, Height))
                            {
                                using (Graphics g = Graphics.FromImage(capture)) g.CopyFromScreen(Location, Point.Empty, Size);
                                if (capture.GetPixel(3, 3).ToArgb() != Color.White.ToArgb()) throw new Exception("Transparent corner is not showing the desktop backdrop.");
                                int colored = 0;
                                for (int y = 0; y < Height; y += 3)
                                    for (int x = 0; x < Width; x += 3)
                                        if (capture.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) colored++;
                                if (colored < 500) throw new Exception("Layered window appears blank.");
                                capture.Save(Path.Combine(directory, "native-desktop.png"), ImageFormat.Png);
                            }
                            results.Add("PASS: actual layered desktop capture is nonblank and transparent");
                            Point corner = PointToScreen(new Point(3, 3));
                            Point face = PointToScreen(new Point(Width / 2, Height / 3));
                            if (Native.SendMessage(Handle, Native.WM_NCHITTEST, IntPtr.Zero, Pack(corner)).ToInt32() != -1)
                                throw new Exception("Transparent hit-test failed.");
                            if (Native.SendMessage(Handle, Native.WM_NCHITTEST, IntPtr.Zero, Pack(face)).ToInt32() != 1)
                                throw new Exception("Character hit-test failed.");
                            results.Add("PASS: native transparent and opaque hit-testing");
                            start = Location;
                            Cursor.Position = face;
                            Native.SendMessage(Handle, 0x201, new IntPtr(1), Pack(PointToClient(face)));
                            Cursor.Position = new Point(face.X + 80, face.Y - 40);
                            Native.SendMessage(Handle, 0x200, new IntPtr(1), Pack(PointToClient(Cursor.Position)));
                            Native.SendMessage(Handle, 0x202, IntPtr.Zero, Pack(PointToClient(Cursor.Position)));
                            if (Location != new Point(start.X + 80, start.Y - 40)) throw new Exception("Native drag failed.");
                            results.Add("PASS: native mouse messages drag the pet by the expected offset");
                            break;
                        case 2:
                            Point click = new Point(Width / 2, Height / 3);
                            Cursor.Position = PointToScreen(click);
                            Native.SendMessage(Handle, 0x201, new IntPtr(1), Pack(click));
                            Native.SendMessage(Handle, 0x202, IntPtr.Zero, Pack(click));
                            if (row != 3 || !oneShot) throw new Exception("Click did not wave.");
                            Native.SendMessage(Handle, 0x203, new IntPtr(1), Pack(click));
                            Native.SendMessage(Handle, 0x202, IntPtr.Zero, Pack(click));
                            if (row != 4 || !oneShot) throw new Exception("Double-click did not jump.");
                            results.Add("PASS: native click waves and native double-click jumps");
                            ChangeSize(2);
                            if (Size != new Size(384, 416)) throw new Exception("Size menu handler failed.");
                            ChangeSize(1.5);
                            pauseItem.PerformClick();
                            if (!paused) throw new Exception("Pause toggle failed.");
                            start = Location;
                            break;
                        case 3:
                            if (Location != start || !paused) throw new Exception("Pause is unstable.");
                            pauseItem.PerformClick();
                            results.Add("PASS: native size changes and pause toggle");
                            StopWalk(); Play(1); start = Location;
                            break;
                        case 4:
                            if (Left <= start.X || row != 1) throw new Exception("Walking did not move right.");
                            results.Add("PASS: walking animation moves the actual desktop window");
                            StopWalk();
                            Native.SendMessage(Handle, 0x205, IntPtr.Zero, Pack(new Point(Width / 2, Height / 3)));
                            if (!menu.Visible) throw new Exception("Right-click menu did not open.");
                            break;
                        case 5:
                            using (Bitmap capture = new Bitmap(backdrop.Width, backdrop.Height))
                            {
                                using (Graphics g = Graphics.FromImage(capture)) g.CopyFromScreen(backdrop.Location, Point.Empty, backdrop.Size);
                                capture.Save(Path.Combine(directory, "native-menu.png"), ImageFormat.Png);
                            }
                            menu.Close();
                            HidePet();
                            if (Visible || timer.Enabled) throw new Exception("Hide failed.");
                            Native.SendMessage(Handle, Native.ShowMessage, IntPtr.Zero, IntPtr.Zero);
                            if (!Visible || !timer.Enabled) throw new Exception("Restore failed.");
                            results.Add("PASS: native right-click menu, hide and single-instance restore message");
                            finish();
                            break;
                    }
                }
                catch (Exception e) { VerificationFailed = true; results.Add("FAIL: " + e); finish(); }
            };
            verifier.Start();
        }
        private static IntPtr Pack(Point p) { return new IntPtr((p.X & 0xffff) | ((p.Y & 0xffff) << 16)); }
    }
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--self-test") return SelfTest(args[1]);
            bool verify = args.Length > 0 && args[0] == "--verify";
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\XiaoXiaoWan.Desktop.1." + Environment.UserName + (verify ? ".Verify" : ""), out created))
            {
                if (!created) { Native.PostMessage(new IntPtr(0xffff), Native.ShowMessage, IntPtr.Zero, IntPtr.Zero); return 0; }
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (Pet pet = new Pet(verify))
                    {
                        if (verify && args.Length == 2) pet.Shown += delegate { pet.Verify(args[1]); };
                        Application.Run(pet);
                        if (pet.VerificationFailed) return 1;
                    }
                    return 0;
                }
                catch (Exception e)
                {
                    MessageBox.Show("小小湾启动失败：\n" + e.Message, "小小湾", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }
            }
        }
        private static int SelfTest(string directory)
        {
            Directory.CreateDirectory(directory);
            List<string> checks = new List<string>();
            try
            {
                Rectangle area = new Rectangle(-1920, 0, 1920, 1040);
                if (Geometry.Clamp(new Point(-3000, 2000), new Size(288, 312), area) != new Point(-1920, 728)) throw new Exception("Negative monitor clamp failed.");
                Point[] points = {new Point(0,-10),new Point(10,0),new Point(0,10),new Point(-10,0)};
                for (int i = 0; i < 4; i++) if (Geometry.Direction(points[i], Point.Empty) != i * 4) throw new Exception("Cardinal gaze failed.");
                checks.Add("PASS: monitor bounds and cardinal gaze");
                using (Sprites sprites = new Sprites())
                {
                    int frames = 0;
                    foreach (double scale in new[] {1.0, 1.5, 2.0, 2.5})
                    {
                        sprites.Resize(scale);
                        for (int row = 0; row < 11; row++)
                            for (int col = 0; col < Sprites.Durations[row].Length; col++)
                            {
                                Bitmap b = sprites.Get(row, col);
                                if (b.GetPixel(0, 0).A != 0) throw new Exception("Nontransparent corner.");
                                int opaque = 0;
                                for (int y = 0; y < b.Height; y += 4)
                                    for (int x = 0; x < b.Width; x += 4) if (b.GetPixel(x, y).A > 20) opaque++;
                                if (opaque < 50) throw new Exception("Blank frame.");
                                frames++;
                            }
                    }
                    checks.Add("PASS: " + frames + " rendered frames across four sizes, alpha and nonblank checks");
                    sprites.Resize(1.5);
                    using (Bitmap sheet = new Bitmap(5 * 288, 2 * 312, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(sheet))
                        {
                            g.Clear(Color.FromArgb(232, 237, 240));
                            for (int i = 0; i < 9; i++) g.DrawImageUnscaled(sprites.Get(i, 0), (i % 5) * 288, (i / 5) * 312);
                            g.DrawImageUnscaled(sprites.Get(9, 4), 4 * 288, 312);
                        }
                        sheet.Save(Path.Combine(directory, "rendered-states.png"), ImageFormat.Png);
                    }
                }
                Preferences p = new Preferences { Scale = 2, X = -400, Y = 500, HasPosition = true, Wander = false };
                using (MemoryStream stream = new MemoryStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Preferences));
                    serializer.WriteObject(stream, p); stream.Position = 0;
                    Preferences roundtrip = (Preferences)serializer.ReadObject(stream);
                    if (roundtrip.Scale != 2 || roundtrip.X != -400 || roundtrip.Wander) throw new Exception("Preferences roundtrip failed.");
                }
                checks.Add("PASS: settings roundtrip");
                File.WriteAllLines(Path.Combine(directory, "self-test.txt"), checks.ToArray());
                return 0;
            }
            catch (Exception e)
            {
                checks.Add("FAIL: " + e); File.WriteAllLines(Path.Combine(directory, "self-test.txt"), checks.ToArray()); return 1;
            }
        }
    }
}
