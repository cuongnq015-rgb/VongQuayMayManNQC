using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VongQuayMayManNQC.Helpers;
using VongQuayMayManNQC.Models;
using MediaPlayer = System.Windows.Media.MediaPlayer;
using WpfPath = System.Windows.Shapes.Path;
using IOPath = System.IO.Path;

namespace VongQuayMayManNQC
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Participant> _participants = new ObservableCollection<Participant>();
        private readonly ObservableCollection<WinnerRecord> _winners = new ObservableCollection<WinnerRecord>();
        private readonly ObservableCollection<PrizeConfig> _prizes = new ObservableCollection<PrizeConfig>();

        private readonly Random _random = new Random();

        private bool _isSpinning = false;
        private double _currentAngle = 0;
        private int _highlightIndex = -1;
        private string _selectedPrizeName = "";

        private const double WheelCanvasSize = 760;
        private const double WheelRadius = 300;
        private const double CenterX = WheelCanvasSize / 2;
        private const double CenterY = WheelCanvasSize / 2;

        private readonly string _stateFile =
            IOPath.Combine(GetAppDataFolder(), "appstate.json");

        private readonly MediaPlayer _spinPlayer = new MediaPlayer();
        private readonly MediaPlayer _winPlayer = new MediaPlayer();

        private readonly List<Ellipse> _lightBulbs = new List<Ellipse>();

        private static string GetAppDataFolder()
        {
            string folder = IOPath.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VongQuayMayManNQC");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }

        public MainWindow()
        {
            InitializeComponent();

            lstParticipants.ItemsSource = _participants;
            dgWinners.ItemsSource = _winners;

            InitPrizeDefaults();
            InitAudio();
            DrawLights();
            LoadStateOrDefault();

            RefreshPrizeCombo();
            RestoreSelectedPrizeAfterLoad();

            DrawWheel();
            UpdatePrizeStatus();
            UpdateAwardedLabels();
            UpdateLastWinnerDisplay();
        }

        #region Init

        private void InitPrizeDefaults()
        {
            _prizes.Clear();
            _prizes.Add(new PrizeConfig { PrizeName = "Giải Nhất", Quantity = 1, AwardedCount = 0 });
            _prizes.Add(new PrizeConfig { PrizeName = "Giải Nhì", Quantity = 2, AwardedCount = 0 });
            _prizes.Add(new PrizeConfig { PrizeName = "Giải Ba", Quantity = 3, AwardedCount = 0 });
            _prizes.Add(new PrizeConfig { PrizeName = "Khuyến khích", Quantity = 5, AwardedCount = 0 });
        }

        private void InitAudio()
        {
            try
            {
                string spinFile = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "spin.mp3");
                string winFile = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "win.mp3");

                if (File.Exists(spinFile))
                    _spinPlayer.Open(new Uri(spinFile, UriKind.Absolute));

                if (File.Exists(winFile))
                    _winPlayer.Open(new Uri(winFile, UriKind.Absolute));
            }
            catch
            {
            }
        }

        private void DrawLights()
        {
            if (LightCanvas == null) return;

            LightCanvas.Children.Clear();
            _lightBulbs.Clear();

            int count = 32;
            double bulbRadius = 8;
            double ringRadius = 344;

            for (int i = 0; i < count; i++)
            {
                double angle = (2 * Math.PI / count) * i - Math.PI / 2;
                double x = CenterX + ringRadius * Math.Cos(angle) - bulbRadius;
                double y = CenterY + ringRadius * Math.Sin(angle) - bulbRadius;

                Ellipse bulb = new Ellipse();
                bulb.Width = bulbRadius * 2;
                bulb.Height = bulbRadius * 2;
                bulb.Fill = (i % 2 == 0) ? Brushes.Gold : Brushes.OrangeRed;
                bulb.Stroke = Brushes.White;
                bulb.StrokeThickness = 1.2;
                bulb.Opacity = 0.85;

                Canvas.SetLeft(bulb, x);
                Canvas.SetTop(bulb, y);
                LightCanvas.Children.Add(bulb);
                _lightBulbs.Add(bulb);

                DoubleAnimation anim = new DoubleAnimation
                {
                    From = 0.35,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(550),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromMilliseconds(i * 45)
                };

                bulb.BeginAnimation(UIElement.OpacityProperty, anim);
            }
        }

        #endregion

        #region Save / Load

        private void SaveState()
        {
            try
            {
                AppState state = new AppState();
                state.Participants = _participants.ToList();
                state.Winners = _winners.ToList();
                state.PrizeConfigs = _prizes.ToList();
                state.RemoveWinnerAfterWin = chkRemoveWinner.IsChecked == true;

                JsonHelper.Save(_stateFile, state);

                File.WriteAllText(IOPath.Combine(GetAppDataFolder(),"selected_prize.txt"),_selectedPrizeName ?? "");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message);
            }
        }


        private void LoadStateOrDefault()
        {
            try
            {
                if (!File.Exists(_stateFile))
                {
                    ApplyPrizeToInputs();
                    return;
                }

                AppState state = JsonHelper.Load<AppState>(_stateFile);
                if (state == null)
                {
                    ApplyPrizeToInputs();
                    return;
                }

                _participants.Clear();
                if (state.Participants != null)
                {
                    foreach (Participant p in state.Participants)
                    {
                        if (p != null)
                            _participants.Add(p);
                    }
                }

                _winners.Clear();
                if (state.Winners != null)
                {
                    foreach (WinnerRecord w in state.Winners)
                    {
                        if (w != null)
                            _winners.Add(w);
                    }
                }

                _prizes.Clear();
                if (state.PrizeConfigs != null && state.PrizeConfigs.Count > 0)
                {
                    foreach (PrizeConfig pr in state.PrizeConfigs)
                    {
                        if (pr != null)
                            _prizes.Add(pr);
                    }
                }
                else
                {
                    InitPrizeDefaults();
                }

                chkRemoveWinner.IsChecked = state.RemoveWinnerAfterWin;
                ApplyPrizeToInputs();

                string selectedPrizeFile =IOPath.Combine(GetAppDataFolder(),"selected_prize.txt");

                if (File.Exists(selectedPrizeFile))
                {
                    _selectedPrizeName = File.ReadAllText(selectedPrizeFile).Trim();
                }
            }
            catch
            {
                InitPrizeDefaults();
                ApplyPrizeToInputs();
            }
        }

        private void RestoreSelectedPrizeAfterLoad()
        {
            if (cmbCurrentPrize == null || cmbCurrentPrize.Items.Count == 0)
                return;

            PrizeConfig selected = null;

            if (!string.IsNullOrWhiteSpace(_selectedPrizeName))
            {
                selected = _prizes.FirstOrDefault(x =>
                    x.PrizeName.Equals(_selectedPrizeName, StringComparison.OrdinalIgnoreCase));
            }

            if (selected == null)
                selected = _prizes.FirstOrDefault();

            cmbCurrentPrize.SelectedItem = selected;
        }

        #endregion

        #region Prize UI

        private void RefreshPrizeCombo()
        {
            if (cmbCurrentPrize == null) return;

            string oldPrizeName = _selectedPrizeName;

            cmbCurrentPrize.ItemsSource = null;
            cmbCurrentPrize.ItemsSource = _prizes;

            if (_prizes.Count == 0)
                return;

            PrizeConfig selected = null;

            if (!string.IsNullOrWhiteSpace(oldPrizeName))
            {
                selected = _prizes.FirstOrDefault(x =>
                    x.PrizeName.Equals(oldPrizeName, StringComparison.OrdinalIgnoreCase));
            }

            if (selected == null)
                selected = _prizes[0];

            cmbCurrentPrize.SelectedItem = selected;
            _selectedPrizeName = selected.PrizeName;
        }

        private void ApplyPrizeToInputs()
        {
            PrizeConfig p1 = GetPrizeByName("Giải Nhất");
            PrizeConfig p2 = GetPrizeByName("Giải Nhì");
            PrizeConfig p3 = GetPrizeByName("Giải Ba");
            PrizeConfig p4 = GetPrizeByName("Khuyến khích");

            txtPrize1Qty.Text = p1 != null ? p1.Quantity.ToString() : "1";
            txtPrize2Qty.Text = p2 != null ? p2.Quantity.ToString() : "2";
            txtPrize3Qty.Text = p3 != null ? p3.Quantity.ToString() : "3";
            txtPrizeKKQty.Text = p4 != null ? p4.Quantity.ToString() : "5";
        }

        private PrizeConfig GetPrizeByName(string prizeName)
        {
            return _prizes.FirstOrDefault(x =>
                x.PrizeName.Equals(prizeName, StringComparison.OrdinalIgnoreCase));
        }

        private bool SavePrizeConfigsFromUI()
        {
            int q1, q2, q3, q4;

            if (!int.TryParse(txtPrize1Qty.Text.Trim(), out q1) || q1 < 0)
            {
                MessageBox.Show("Số lượng Giải Nhất không hợp lệ.");
                return false;
            }
            if (!int.TryParse(txtPrize2Qty.Text.Trim(), out q2) || q2 < 0)
            {
                MessageBox.Show("Số lượng Giải Nhì không hợp lệ.");
                return false;
            }
            if (!int.TryParse(txtPrize3Qty.Text.Trim(), out q3) || q3 < 0)
            {
                MessageBox.Show("Số lượng Giải Ba không hợp lệ.");
                return false;
            }
            if (!int.TryParse(txtPrizeKKQty.Text.Trim(), out q4) || q4 < 0)
            {
                MessageBox.Show("Số lượng Khuyến khích không hợp lệ.");
                return false;
            }

            UpdatePrize("Giải Nhất", q1);
            UpdatePrize("Giải Nhì", q2);
            UpdatePrize("Giải Ba", q3);
            UpdatePrize("Khuyến khích", q4);

            UpdateAwardedLabels();
            RefreshPrizeCombo();
            UpdatePrizeStatus();
            SaveState();

            return true;
        }

        private void UpdatePrize(string name, int quantity)
        {
            PrizeConfig p = GetPrizeByName(name);
            if (p == null)
            {
                p = new PrizeConfig();
                p.PrizeName = name;
                p.Quantity = quantity;
                p.AwardedCount = 0;
                _prizes.Add(p);
            }
            else
            {
                p.Quantity = quantity;
            }
        }

        private void UpdateAwardedLabels()
        {
            PrizeConfig p1 = GetPrizeByName("Giải Nhất");
            PrizeConfig p2 = GetPrizeByName("Giải Nhì");
            PrizeConfig p3 = GetPrizeByName("Giải Ba");
            PrizeConfig p4 = GetPrizeByName("Khuyến khích");

            txtPrize1Awarded.Text = p1 != null ? p1.AwardedCount.ToString() : "0";
            txtPrize2Awarded.Text = p2 != null ? p2.AwardedCount.ToString() : "0";
            txtPrize3Awarded.Text = p3 != null ? p3.AwardedCount.ToString() : "0";
            txtPrizeKKAwarded.Text = p4 != null ? p4.AwardedCount.ToString() : "0";
        }

        private void UpdatePrizeStatus()
        {
            PrizeConfig prize = cmbCurrentPrize != null ? cmbCurrentPrize.SelectedItem as PrizeConfig : null;
            if (prize == null)
            {
                txtPrizeStatus.Text = "Chưa có dữ liệu giải thưởng";
                txtCurrentPrizeBadge.Text = "Chưa chọn giải";
                return;
            }

            _selectedPrizeName = prize.PrizeName;

            txtPrizeStatus.Text = prize.PrizeName + ": còn " + prize.Remaining +
                                  " / tổng " + prize.Quantity +
                                  " (đã quay " + prize.AwardedCount + ")";
            txtCurrentPrizeBadge.Text = prize.PrizeName;
        }

        #endregion

        #region Wheel Engine PRO TANGENTIAL

        private void DrawWheel()
        {
            if (WheelCanvas == null) return;

            WheelCanvas.Children.Clear();

            List<Participant> allParticipants = _participants.ToList();
            int count = allParticipants.Count;

            if (count <= 0)
            {
                DrawEmptyWheel();
                return;
            }

            double sliceAngle = 360.0 / count;

            Color[] palette = new Color[]
            {
                Color.FromRgb(239, 68, 68),
                Color.FromRgb(245, 158, 11),
                Color.FromRgb(234, 179, 8),
                Color.FromRgb(132, 204, 22),
                Color.FromRgb(34, 197, 94),
                Color.FromRgb(20, 184, 166),
                Color.FromRgb(14, 165, 233),
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(99, 102, 241),
                Color.FromRgb(168, 85, 247),
                Color.FromRgb(236, 72, 153),
                Color.FromRgb(217, 70, 239)
            };

            for (int i = 0; i < count; i++)
            {
                Participant participant = allParticipants[i];
                bool isWinnerBefore = participant.IsWinner;

                double start = i * sliceAngle - 90;
                double end = start + sliceAngle;

                DrawSlice(i, start, end, participant, palette[i % palette.Length], isWinnerBefore, count);
            }

            DrawCenterCap();
        }

        private void DrawCenterCap()
        {
            Ellipse outer = new Ellipse
            {
                Width = 70,
                Height = 70,
                Fill = Brushes.WhiteSmoke,
                Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                StrokeThickness = 3
            };
            Canvas.SetLeft(outer, CenterX - 35);
            Canvas.SetTop(outer, CenterY - 35);
            WheelCanvas.Children.Add(outer);

            Ellipse inner = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(inner, CenterX - 9);
            Canvas.SetTop(inner, CenterY - 9);
            WheelCanvas.Children.Add(inner);
        }

        private void DrawEmptyWheel()
        {
            Ellipse emptyCircle = new Ellipse
            {
                Width = WheelRadius * 2,
                Height = WheelRadius * 2,
                Fill = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Stroke = Brushes.White,
                StrokeThickness = 3
            };

            Canvas.SetLeft(emptyCircle, CenterX - WheelRadius);
            Canvas.SetTop(emptyCircle, CenterY - WheelRadius);
            WheelCanvas.Children.Add(emptyCircle);

            FormattedText ft = new FormattedText(
                "CHƯA CÓ NGƯỜI THAM GIA",
                CultureInfo.GetCultureInfo("vi-VN"),
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                28,
                Brushes.White,
                1.0);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawText(ft, new Point(CenterX - ft.Width / 2, CenterY - ft.Height / 2));
            }

            AddVisualToWheel(dv, new Point(CenterX, CenterY));
        }

        private void DrawSlice(
            int index,
            double startAngle,
            double endAngle,
            Participant participant,
            Color baseColor,
            bool isWinnerBefore,
            int totalCount)
        {
            bool isLargeArc = (endAngle - startAngle) > 180;

            Point p1 = PointOnCircle(CenterX, CenterY, WheelRadius, startAngle);
            Point p2 = PointOnCircle(CenterX, CenterY, WheelRadius, endAngle);

            PathFigure pf = new PathFigure();
            pf.StartPoint = new Point(CenterX, CenterY);
            pf.Segments.Add(new LineSegment(p1, true));
            pf.Segments.Add(new ArcSegment(
                p2,
                new Size(WheelRadius, WheelRadius),
                0,
                isLargeArc,
                SweepDirection.Clockwise,
                true));
            pf.IsClosed = true;

            PathGeometry pg = new PathGeometry();
            pg.Figures.Add(pf);

            Color fillColor = baseColor;
            if (isWinnerBefore)
                fillColor = Darken(fillColor, 0.45);

            WpfPath path = new WpfPath
            {
                Data = pg,
                Stroke = Brushes.White,
                StrokeThickness = (_highlightIndex == index) ? 4 : 2,
                Fill = new SolidColorBrush(fillColor),
                Opacity = (_highlightIndex == index) ? 1.0 : 0.98
            };

            WheelCanvas.Children.Add(path);

            DrawNameOnSlice(index, startAngle, endAngle, participant.Name, isWinnerBefore, totalCount);
        }

        /// <summary>
        /// Kiểu chữ giống ảnh:
        /// - 1 dòng
        /// - chạy theo lát cắt (tangential)
        /// - bên phải ngang, lên trên dọc, qua trái lật ngược
        /// - nằm vùng giữa/ngoài của lát
        /// </summary>
        private void DrawNameOnSlice(
            int sliceIndex,
            double startAngle,
            double endAngle,
            string text,
            bool isWinnerBefore,
            int totalCount)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            text = NormalizeDisplayName(text.Trim());
            if (text.Length == 0)
                return;

            double sliceAngle = endAngle - startAngle;
            double midAngle = startAngle + sliceAngle / 2.0;

            Brush textBrush = isWinnerBefore ? Brushes.Gainsboro : Brushes.White;
            if (_highlightIndex == sliceIndex)
                textBrush = Brushes.Yellow;

            // Vùng đặt text giống ảnh: không sát tâm, cũng không sát viền
            double textRadius = GetTangentialTextRadius(totalCount);

            // Độ rộng lát cắt tại vị trí đặt chữ
            double halfAngleRad = (sliceAngle / 2.0) * Math.PI / 180.0;
            double sliceWidthAtText = 2.0 * textRadius * Math.Sin(halfAngleRad);

            // Bề ngang text sau khi xoay
            double maxTextThickness = Math.Max(16, sliceWidthAtText - GetTangentialSidePadding(totalCount));

            // Chiều dài text chạy dọc theo lát
            double innerRadius = WheelRadius * 0.30;
            double outerRadius = WheelRadius * 0.90;
            double maxTextLength = Math.Max(40, outerRadius - innerRadius);

            double fontSize = GetTangentialFontSize(totalCount);

            while (fontSize >= 6.0)
            {
                FormattedText ftTest = CreateFormattedText(text, fontSize, Brushes.White);

                // Sau khi xoay tangential:
                // chiều dài theo lát ~ ft.Width
                // độ dày theo lát ~ ft.Height
                if (ftTest.Width <= maxTextLength && ftTest.Height <= maxTextThickness)
                    break;

                fontSize -= 0.5;
            }

            FormattedText ft = CreateFormattedText(text, fontSize, textBrush);

            // Góc xoay theo lát cắt, KHÔNG phải radial.
            // midAngle sẽ cho ra kiểu giống ảnh:
            // phải = ngang, trên = dọc, trái = lật ngược
            double rotateAngle = midAngle;

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.PushTransform(new RotateTransform(rotateAngle, ft.Width / 2.0, ft.Height / 2.0));
                dc.DrawText(ft, new Point(0, 0));
                dc.Pop();
            }

            Point textCenter = PointOnCircle(CenterX, CenterY, textRadius, midAngle);
            AddVisualToWheel(visual, textCenter);
        }

        private string NormalizeDisplayName(string text)
        {
            // Ảnh mẫu là 1 dòng, nên giữ nguyên 1 dòng.
            // Nếu user nhập nhiều space thì gọn lại.
            string[] parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        private double GetTangentialTextRadius(int count)
        {
            if (count <= 8) return WheelRadius * 0.66;
            if (count <= 12) return WheelRadius * 0.64;
            if (count <= 16) return WheelRadius * 0.63;
            if (count <= 20) return WheelRadius * 0.62;
            if (count <= 24) return WheelRadius * 0.61;
            if (count <= 32) return WheelRadius * 0.60;
            if (count <= 40) return WheelRadius * 0.59;
            return WheelRadius * 0.58;
        }

        private double GetTangentialSidePadding(int count)
        {
            if (count <= 12) return 16;
            if (count <= 20) return 14;
            if (count <= 32) return 12;
            return 10;
        }

        private double GetTangentialFontSize(int count)
        {
            if (count <= 6) return 28;
            if (count <= 8) return 24;
            if (count <= 10) return 22;
            if (count <= 12) return 20;
            if (count <= 14) return 18;
            if (count <= 16) return 17;
            if (count <= 20) return 15;
            if (count <= 24) return 13;
            if (count <= 28) return 12;
            if (count <= 32) return 11;
            if (count <= 40) return 10;
            if (count <= 50) return 9;
            return 8;
        }

        private FormattedText CreateFormattedText(string text, double fontSize, Brush brush)
        {
            return new FormattedText(
                text,
                CultureInfo.GetCultureInfo("vi-VN"),
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                fontSize,
                brush,
                1.0);
        }

        #region Helpers draw

        private void AddVisualToWheel(DrawingVisual visual, Point centerPoint)
        {
            if (visual == null) return;

            Rect bounds = visual.ContentBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            DrawingImage image = new DrawingImage(visual.Drawing);
            Image img = new Image();
            img.Source = image;
            img.Width = bounds.Width;
            img.Height = bounds.Height;
            img.IsHitTestVisible = false;
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            Canvas.SetLeft(img, centerPoint.X - bounds.Width / 2.0);
            Canvas.SetTop(img, centerPoint.Y - bounds.Height / 2.0);

            WheelCanvas.Children.Add(img);
        }

        private Color Darken(Color color, double factor)
        {
            byte r = (byte)Math.Max(0, Math.Min(255, color.R * factor));
            byte g = (byte)Math.Max(0, Math.Min(255, color.G * factor));
            byte b = (byte)Math.Max(0, Math.Min(255, color.B * factor));
            return Color.FromRgb(r, g, b);
        }

        private Point PointOnCircle(double cx, double cy, double radius, double angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            return new Point(
                cx + radius * Math.Cos(rad),
                cy + radius * Math.Sin(rad));
        }

        #endregion

        #endregion

        #region Winner / Status

        private void UpdateLastWinnerDisplay()
        {
            WinnerRecord last = _winners.LastOrDefault();
            if (last == null)
            {
                txtLastWinner.Text = "Chưa có người trúng";
                txtLastPrize.Text = "-";
                return;
            }

            txtLastWinner.Text = last.Name;
            txtLastPrize.Text = last.PrizeName;
        }

        private List<Participant> GetEligibleParticipants()
        {
            bool removeWinner = chkRemoveWinner.IsChecked == true;

            if (!removeWinner)
                return _participants.ToList();

            return _participants.Where(p => !p.IsWinner).ToList();
        }

        private int FindWheelIndexByName(string name)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                if (_participants[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        #endregion

        #region Buttons - Participant

        private void BtnAddParticipant_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNameInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên người tham gia.");
                return;
            }

            if (_participants.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Tên này đã tồn tại.");
                return;
            }

            _participants.Add(new Participant { Name = name, IsWinner = false });
            txtNameInput.Clear();

            _highlightIndex = -1;
            DrawWheel();
            SaveState();
        }

        private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            Participant p = lstParticipants.SelectedItem as Participant;
            if (p == null)
            {
                MessageBox.Show("Chọn người cần xóa.");
                return;
            }

            if (MessageBox.Show("Xóa '" + p.Name + "'?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _participants.Remove(p);
                _highlightIndex = -1;
                DrawWheel();
                SaveState();
            }
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel files (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls";

            if (ofd.ShowDialog() != true) return;

            try
            {
                List<string> names = ExcelHelper.ReadNamesFromExcel(ofd.FileName);
                int added = 0;

                foreach (string n in names)
                {
                    if (!_participants.Any(p => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                    {
                        _participants.Add(new Participant
                        {
                            Name = n,
                            IsWinner = false
                        });
                        added++;
                    }
                }

                DrawWheel();
                SaveState();
                MessageBox.Show("Đã nhập " + added + " người.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi nhập Excel: " + ex.Message);
            }
        }

        #endregion

        #region Buttons - Prize

        private void BtnSavePrizeConfig_Click(object sender, RoutedEventArgs e)
        {
            if (SavePrizeConfigsFromUI())
                MessageBox.Show("Đã lưu cơ cấu giải.");
        }

        private void CmbCurrentPrize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PrizeConfig prize = cmbCurrentPrize.SelectedItem as PrizeConfig;
            if (prize != null)
                _selectedPrizeName = prize.PrizeName;

            UpdatePrizeStatus();
            SaveState();
        }

        #endregion

        #region Buttons - Spin

        private async void BtnSpin_Click(object sender, RoutedEventArgs e)
        {
            if (!SavePrizeConfigsFromUI()) return;

            PrizeConfig prize = cmbCurrentPrize.SelectedItem as PrizeConfig;
            if (prize == null)
            {
                MessageBox.Show("Hãy chọn giải cần quay.");
                return;
            }

            await SpinForPrizeAsync(prize.PrizeName);
        }

        private async void BtnSpinCurrentPrize_Click(object sender, RoutedEventArgs e)
        {
            if (!SavePrizeConfigsFromUI()) return;

            PrizeConfig prize = cmbCurrentPrize.SelectedItem as PrizeConfig;
            if (prize == null)
            {
                MessageBox.Show("Chưa chọn giải.");
                return;
            }

            if (prize.Remaining <= 0)
            {
                MessageBox.Show(prize.PrizeName + " đã đủ số lượng.");
                return;
            }

            int remain = prize.Remaining;
            for (int i = 0; i < remain; i++)
            {
                List<Participant> eligible = GetEligibleParticipants();
                if (eligible.Count == 0)
                {
                    MessageBox.Show("Đã hết người đủ điều kiện để quay.");
                    break;
                }

                await SpinForPrizeAsync(prize.PrizeName);
                await Task.Delay(500);
            }
        }

        private async void BtnSpinAll_Click(object sender, RoutedEventArgs e)
        {
            if (!SavePrizeConfigsFromUI()) return;

            foreach (PrizeConfig prize in _prizes.ToList())
            {
                while (prize.Remaining > 0)
                {
                    List<Participant> eligible = GetEligibleParticipants();
                    if (eligible.Count == 0)
                    {
                        MessageBox.Show("Đã hết người đủ điều kiện để quay.");
                        return;
                    }

                    cmbCurrentPrize.SelectedItem = prize;
                    _selectedPrizeName = prize.PrizeName;
                    UpdatePrizeStatus();

                    await SpinForPrizeAsync(prize.PrizeName);
                    await Task.Delay(500);
                }
            }

            MessageBox.Show("Đã quay xong toàn bộ cơ cấu giải.");
        }

        private async Task SpinForPrizeAsync(string prizeName)
        {
            if (_isSpinning) return;

            PrizeConfig prize = GetPrizeByName(prizeName);
            if (prize == null)
            {
                MessageBox.Show("Không tìm thấy giải.");
                return;
            }

            if (prize.Remaining <= 0)
            {
                MessageBox.Show(prize.PrizeName + " đã hết lượt.");
                return;
            }

            List<Participant> eligible = GetEligibleParticipants();
            if (eligible.Count == 0)
            {
                MessageBox.Show("Không còn người đủ điều kiện.");
                return;
            }

            _isSpinning = true;
            btnSpinCenter.IsEnabled = false;

            try
            {
                Participant winner = eligible[_random.Next(eligible.Count)];

                int winnerWheelIndex = FindWheelIndexByName(winner.Name);
                if (winnerWheelIndex < 0)
                    winnerWheelIndex = 0;

                int totalCount = _participants.Count;
                double sliceAngle = 360.0 / totalCount;

                // Góc giữa của lát trúng trên vòng quay gốc
                double winnerCenterAngle = (winnerWheelIndex * sliceAngle) - 90 + (sliceAngle / 2.0);

                // Mũi tên nằm trên đỉnh (12h = -90)
                // Ta cần xoay wheel sao cho tâm lát trúng chạy về -90
                double targetRotationNormalized = NormalizeAngle(-90 - winnerCenterAngle);

                double currentNormalized = NormalizeAngle(_currentAngle);
                double delta = targetRotationNormalized - currentNormalized;
                if (delta < 0) delta += 360;

                double extraSpins = 360 * _random.Next(6, 9);
                double finalAngle = _currentAngle + extraSpins + delta;

                await AnimateWheelAsync(finalAngle);

                prize.AwardedCount++;

                Participant realWinner = _participants.FirstOrDefault(x =>
                    x.Name.Equals(winner.Name, StringComparison.OrdinalIgnoreCase));
                if (realWinner != null)
                    realWinner.IsWinner = true;

                WinnerRecord record = new WinnerRecord();
                record.STT = _winners.Count + 1;
                record.Name = winner.Name;
                record.PrizeName = prize.PrizeName;
                record.WinTime = DateTime.Now;
                _winners.Add(record);

                _highlightIndex = winnerWheelIndex;

                DrawWheel();
                UpdateAwardedLabels();
                UpdateLastWinnerDisplay();
                UpdatePrizeStatus();
                SaveState();

                PlayWinSound();
                ShowWinnerEffect(winner.Name, prize.PrizeName);
            }
            finally
            {
                _isSpinning = false;
                btnSpinCenter.IsEnabled = true;
            }
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 360;
            if (angle < 0) angle += 360;
            return angle;
        }

        private async Task AnimateWheelAsync(double finalAngle)
        {
            PlaySpinSound();

            try
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                DoubleAnimation anim = new DoubleAnimation
                {
                    From = _currentAngle,
                    To = finalAngle,
                    Duration = TimeSpan.FromSeconds(5.2),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                anim.Completed += delegate
                {
                    _currentAngle = NormalizeAngle(finalAngle);
                    if (wheelRotate != null)
                        wheelRotate.Angle = _currentAngle;

                    tcs.TrySetResult(true);
                };

                wheelRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
                await tcs.Task;
            }
            finally
            {
                StopSpinSound();
            }
        }

        private void ShowWinnerEffect(string winnerName, string prizeName)
        {
            WinnerWindow ww = new WinnerWindow(winnerName, prizeName);
            ww.Owner = this;
            ww.ShowDialog();
        }

        #endregion

        #region Sound

        private void PlaySpinSound()
        {
            try
            {
                _spinPlayer.Position = TimeSpan.Zero;
                _spinPlayer.Play();
            }
            catch
            {
            }
        }

        private void StopSpinSound()
        {
            try
            {
                _spinPlayer.Stop();
            }
            catch
            {
            }
        }

        private void PlayWinSound()
        {
            try
            {
                _winPlayer.Position = TimeSpan.Zero;
                _winPlayer.Play();
            }
            catch
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
        }

        #endregion

        #region Buttons - Export / Reset

        private void BtnExportWinners_Click(object sender, RoutedEventArgs e)
        {
            if (_winners.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu người trúng.");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel file (*.xlsx)|*.xlsx";
            sfd.FileName = "DanhSachTrungThuong.xlsx";

            if (sfd.ShowDialog() != true)
                return;

            try
            {
                ExcelHelper.ExportWinners(sfd.FileName, _winners.ToList());
                MessageBox.Show("Đã xuất file Excel thành công.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message);
            }
        }

        private void BtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Reset toàn bộ dữ liệu người tham gia, lịch sử trúng và cơ cấu giải?",
                "Xác nhận reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            _participants.Clear();
            _winners.Clear();
            _prizes.Clear();

            InitPrizeDefaults();
            chkRemoveWinner.IsChecked = true;
            _selectedPrizeName = "";
            _highlightIndex = -1;
            _currentAngle = 0;
            if (wheelRotate != null)
                wheelRotate.Angle = 0;

            ApplyPrizeToInputs();
            RefreshPrizeCombo();
            RestoreSelectedPrizeAfterLoad();
            DrawWheel();
            UpdatePrizeStatus();
            UpdateAwardedLabels();
            UpdateLastWinnerDisplay();

            try
            {
                if (File.Exists(_stateFile))
                    File.Delete(_stateFile);

                string selectedPrizeFile =IOPath.Combine(GetAppDataFolder(),"selected_prize.txt");

                if (File.Exists(selectedPrizeFile))
                    File.Delete(selectedPrizeFile);
            }
            catch
            {
            }

            SaveState();
            MessageBox.Show("Đã reset toàn bộ.");
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            SaveState();
            base.OnClosed(e);
        }
    }
}