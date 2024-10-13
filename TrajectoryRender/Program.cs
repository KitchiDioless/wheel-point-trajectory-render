using System;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace WheelPointTrajectoryZedGraphApp
{
    public partial class MainForm : Form
    {
        private TextBox txtRadius;
        private TextBox txtVelocity;
        private TextBox AnimationTime;

        private ZedGraphControl zedGraphControl1;

        private double radius = 1.0;
        private double velocity = 1.0;

        private PointPairList listOfTrajectoryPoints = new PointPairList();

        private int windowWidth = 960;
        private int windowHeight = 540;

        private System.Windows.Forms.Timer animationTimer;
        private double currentTime = 0;
        private double timeStep = 0.1;
        private bool isAnimating = false;

        private double animationTimeValue = 10.0;

        private Button btnStartAnimation;

        private const int MaxPoints = 100;


        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            InitializeComponent();
            SetupGraphs();
            SetupInputControls();
            ApplyCustomStyles();
            UpdateGraphs();

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50;
            animationTimer.Tick += AnimationTimer_Tick;

            this.DoubleBuffered = true;
        }

        private void InitializeComponent()
        {
            this.zedGraphControl1 = new ZedGraphControl();

            this.txtRadius = new TextBox 
            {
                Location = new Point(windowWidth - 112, 20), Width = 100, PlaceholderText = "Radius (m)" 
            };

            this.txtVelocity = new TextBox 
            {
                Location = new Point(windowWidth - 112, 60), Width = 100, PlaceholderText = "Velocity (m/s)" 
            };

            this.AnimationTime = new TextBox
            {
                Location = new Point(windowWidth - 112, 100), Width = 100, PlaceholderText = "Animation Time (s)" 
            };

            this.btnStartAnimation = new Button 
            { 
                Location = new Point(windowWidth - 112, 140), Width = 100, Text = "Start Animation" 
            };

            this.SuspendLayout();

            this.zedGraphControl1.Location = new Point(12, 12);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.Size = new Size(windowWidth - 160, windowHeight - 80);
            this.zedGraphControl1.TabIndex = 0;

            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(windowWidth, windowHeight);
            this.Controls.Add(this.zedGraphControl1);
            this.Controls.Add(this.txtRadius);
            this.Controls.Add(this.txtVelocity);
            this.Controls.Add(this.btnStartAnimation);
            this.Controls.Add(this.AnimationTime);
            this.Name = "MainForm";
            this.Text = "Wheel Trajectory Visualization";
            this.ResumeLayout(false);
            this.PerformLayout();

            this.btnStartAnimation.Click += (sender, e) => StartAnimation();
        }

        private void SetupInputControls()
        {
            txtRadius.TextChanged += (sender, e) => UpdateGraphs();
            txtVelocity.TextChanged += (sender, e) => UpdateGraphs();
            AnimationTime.TextChanged += (sender, e) => UpdateGraphs();
        }

        private void ApplyCustomStyles()
        {
            this.BackColor = Color.FromArgb(35, 35, 45);
            SetTextBoxStyles(txtRadius);
            SetTextBoxStyles(txtVelocity);
            SetTextBoxStyles(AnimationTime);
            SetButtonStyles(btnStartAnimation);
            SetGraphControlStyles(zedGraphControl1);
        }

        private void SetTextBoxStyles(TextBox textBox)
        {
            textBox.BackColor = Color.FromArgb(30, 30, 40);
            textBox.ForeColor = Color.White;
            textBox.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void SetButtonStyles(Button button)
        {
            button.BackColor = Color.FromArgb(30, 30, 40);
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        }

        private void SetGraphControlStyles(ZedGraphControl zedGraphControl)
        {
            zedGraphControl.BackColor = Color.FromArgb(20, 20, 30);
        }

        private void SetupGraphs()
        {
            SetupGraph(zedGraphControl1, "Trajectory of Point on Wheel", "X (m)", "Y (m)");
        }

        private void SetupGraph(ZedGraphControl zedGraphControl,
            string title, string xAxisTitle, string yAxisTitle)
        {
            GraphPane pane = zedGraphControl.GraphPane;

            pane.Title.Text = title;
            pane.Title.FontSpec.FontColor = Color.White;

            pane.XAxis.Title.Text = xAxisTitle;
            pane.YAxis.Title.Text = yAxisTitle;

            pane.Fill = new Fill(Color.FromArgb(30, 30, 40));
            pane.Chart.Fill = new Fill(Color.FromArgb(30, 30, 40));

            pane.XAxis.Color = Color.White;
            pane.YAxis.Color = Color.White;

            pane.XAxis.Title.FontSpec.FontColor = Color.White;
            pane.YAxis.Title.FontSpec.FontColor = Color.White;

            pane.XAxis.Scale.FontSpec.FontColor = Color.White;
            pane.YAxis.Scale.FontSpec.FontColor = Color.White;

            pane.XAxis.MajorGrid.Color = Color.Gray;
            pane.YAxis.MajorGrid.Color = Color.Gray;

            pane.XAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.IsVisible = true;

            pane.Chart.Border.Color = Color.White;
            pane.Chart.Border.Width = 2;
            pane.Chart.Border.IsAntiAlias = true;
        }

        private void UpdateGraphs()
        {
            if (!double.TryParse(txtRadius.Text, out radius)) radius = 1.0;
            if (!double.TryParse(txtVelocity.Text, out velocity)) velocity = 1.0;
            if (!double.TryParse(AnimationTime.Text, out animationTimeValue)) animationTimeValue = 10.0;

            listOfTrajectoryPoints.Clear();

            CalculateTrajectory();

            UpdateGraph(zedGraphControl1, listOfTrajectoryPoints);
        }

        private void UpdateGraph(ZedGraphControl zedGraphControl, PointPairList points)
        {
            GraphPane pane = zedGraphControl.GraphPane;
            pane.CurveList.Clear();

            LineItem curve = pane.AddCurve("Trajectory", points,
                Color.FromArgb(199, 54, 89), SymbolType.None);
            curve.Line.Width = 2.5F;
            curve.Line.IsAntiAlias = true;
            curve.Line.IsSmooth = true;

            double centerX = currentTime - (MaxPoints * timeStep) / 2;

            pane.XAxis.Scale.Min = centerX * velocity;
            pane.XAxis.Scale.Max = (centerX + (MaxPoints * timeStep)) * velocity;

            zedGraphControl.AxisChange();
            zedGraphControl.Invalidate();
        }

        private void CalculateTrajectory()
        {
            double totalTime = currentTime;
            for (double t = 0; t <= totalTime; t += timeStep)
            {
                double x = velocity * t;
                
                double theta = (x / radius);
                double y = radius * Math.Sin(theta);

                listOfTrajectoryPoints.Add(x, Math.Abs(y));
            }

            while (listOfTrajectoryPoints.Count > MaxPoints)
            {
                listOfTrajectoryPoints.RemoveAt(0);
            }
        }

        private void StartAnimation()
        {
            currentTime = 0;
            isAnimating = true;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!isAnimating) return;

            currentTime += timeStep;

            listOfTrajectoryPoints.Clear();

            CalculateTrajectory();

            UpdateGraph(zedGraphControl1, listOfTrajectoryPoints);

            zedGraphControl1.Invalidate();

            if (currentTime >= animationTimeValue)
            {
                isAnimating = false;
                animationTimer.Stop();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isAnimating)
            {
                UpdateCirclePosition(e.Graphics);
            }
        }

        private void UpdateCirclePosition(Graphics g)
        {
            Pen pen = new Pen(Color.Blue, 2);

            double x = velocity * currentTime;
            double theta = (x / radius);
            double circleX = x;
            double circleY = radius * Math.Sin(theta);

            g.DrawEllipse(pen, (float)(circleX - radius),
                (float)(circleY - radius), (float)(radius * 2), (float)(radius * 2));

            pen.Dispose();
        }
    }
}
