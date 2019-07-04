using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using TagBites.WinSchedulers;
using TagBites.WinSchedulers.Descriptors;

namespace BucketSchedulerCustomization
{
    public class SchedulerDataSource : BucketSchedulerDataSource
    {
        private string[] _rows;
        private ColumnModel[] _columns;
        private readonly IDictionary<(object, object), BucketModel> _buckets = new Dictionary<(object, object), BucketModel>();

        protected override BucketSchedulerSummaryDescriptor CreateColumnSummaryDescriptor() => new BucketSchedulerSummaryDescriptor(typeof(object)) { TextDelegate = GetText };
        protected override BucketSchedulerBucketDescriptor CreateBucketDescriptor()
        {
            return new BucketSchedulerBucketDescriptor(typeof(BucketModel), nameof(BucketModel.RowResource), nameof(BucketModel.ColumnResource))
            {
                CapacityMember = nameof(BucketModel.Capacity),
                BarColorMember = nameof(BucketModel.BarColor)
            };
        }
        protected override BucketSchedulerTaskDescriptor CreateTaskDescriptor()
        {
            return new BucketSchedulerTaskDescriptor(typeof(TaskModel), nameof(TaskModel.Bucket))
            {
                ConsumptionMember = nameof(TaskModel.Consumption),
                ColorMember = nameof(TaskModel.Color),
                FontColorMember = nameof(TaskModel.FontColor),
                BorderColorMember = nameof(TaskModel.BorderColor),
                GroupMember = nameof(TaskModel.Group)
            };
        }

        public override IList<object> LoadRows() => (_rows ?? (_rows = GenerateRows())).Cast<object>().ToList();
        public override IList<object> LoadColumns() => (_columns ?? (_columns = GenerateColumns())).Cast<object>().ToList();
        public override void LoadContent(BucketSchedulerDataSourceView view)
        {
            var rows = view.Rows;
            var columns = view.Columns;

            foreach (var row in rows)
                foreach (var column in columns)
                {
                    if (!_buckets.ContainsKey((row, column)))
                        _buckets.Add((row, column), GenerateBucket((string)row, (ColumnModel)column));

                    var bucket = _buckets[(row, column)];
                    view.AddBucket(bucket);
                    foreach (var task in bucket.Tasks)
                        view.AddTask(task);
                }
        }

        private string GetText(object model)
        {
            var column = (ColumnModel) model;
            var capacity = 0.0;
            var usedCapacity = 0.0;
            foreach (var row in _rows)
            {
                if (_buckets.ContainsKey((row, column)))
                {
                    var bucket = _buckets[(row, column)];
                    usedCapacity += bucket.UsedCapacity;
                    capacity += bucket.Capacity;
                }
            }       

            return $"{(usedCapacity/ capacity) * 100:n}% - {usedCapacity:n} / {capacity:n}";
        }

        #region Data generation

        private readonly Random m_random = new Random();
        private readonly Color[] m_colors =
        {
            Color.FromRgb(178, 191, 229),
            Color.FromRgb(178,223, 229),
            Color.FromRgb(178, 229, 203),
            Color.FromRgb(184, 229, 178),
            Color.FromRgb(197, 178, 229),
            Color.FromRgb(216, 229, 178),
            Color.FromRgb(229, 178, 178),
            Color.FromRgb(229,178,197),
            Color.FromRgb(229, 178, 229),
            Color.FromRgb(229, 210, 178),
        };

        private string[] GenerateRows()
        {
            return new[]
            {
                "[A] Cutting Station",
                "[B] Preparation Station",
                "[C] Bonding Station",
                "[D] Testing Station",
                "[E] Painting/Lacquering Station",
                "[F] Decorating Station",
                "[G] Controlling Station",
                "[H] Packing Station"
            };
        }
        private ColumnModel[] GenerateColumns()
        {
            var date = DateTime.Now.Date;
            return Enumerable.Range(0, 365).Select(x => new ColumnModel {Date = date + TimeSpan.FromDays(x)}).ToArray();
        }
        private BucketModel GenerateBucket(string row, ColumnModel column)
        {
            var bucket = new BucketModel
            {
                RowResource = row,
                ColumnResource = column,
                Capacity = m_random.NextDouble() * 100
            };

            var count = m_random.Next(0, 10);
            for (var i = 0; i < count; i++)
            {
                var groupId = m_random.Next(0, m_colors.Length - 1);
                var color = m_colors[groupId];
                var borderColor = Lerp(color, Colors.Black, 0.2f);
                var fontColor = Color.FromRgb(110, 110, 110);

                var maxConsumption = m_random.NextDouble() * 50;
                var consumption = Math.Min(m_random.NextDouble() * 10, maxConsumption);
                var startDate = column.Date + TimeSpan.FromHours(6);
                var endDate = column.Date + TimeSpan.FromHours(6 + m_random.Next(12));
                var sb = new StringBuilder();
                sb.AppendLine($"Order: {groupId}/ZLP");
                sb.AppendLine($"Workplace: {row}");
                sb.AppendLine($"Date: {startDate:yyyy-MM-dd HH:mm} - {endDate:yyyy-MM-dd HH:mm}  ({(endDate - startDate).TotalHours:0.00} RBH)");
                sb.AppendLine($"Planned quantity: {consumption:#,0.00} units / {maxConsumption:#,0.00} units  ({(consumption/ maxConsumption) * 100:0.00}%)");

                bucket.Tasks.Add(new TaskModel
                {
                    ID = i,
                    Bucket = bucket,
                    Group = groupId,
                    Color = color,
                    BorderColor = borderColor,
                    FontColor = fontColor,
                    Consumption = consumption,
                    Text = sb.ToString()
                });
            }

            return bucket;
        }

        private static Color Lerp(Color color, Color to, float amount)
        {
            return Color.FromRgb(
                (byte)(color.R + (to.R - color.R) * amount),
                (byte)(color.G + (to.G - color.G) * amount),
                (byte)(color.B + (to.B - color.B) * amount));

        }

        #endregion

        #region Classes

        private class ColumnModel
        {
            public DateTime Date { get; set; }

            public override string ToString() => Date.ToString("yyyy-MM-dd");
        }
        private class BucketModel
        {
            public int ID { get; set; }
            public object RowResource { get; set; }
            public object ColumnResource { get; set; }
            public List<TaskModel> Tasks { get; } = new List<TaskModel>();

            public double Capacity { get; set; }
            public double UsedCapacity => Tasks.Sum(x => x.Consumption);
            public double FreeCapacity => Math.Max(0, Capacity - UsedCapacity);
            public double UsedCapacityPercent => Capacity == 0 ? 0 : (UsedCapacity / Capacity);
            public bool IsOverloaded => FreeCapacity < 0;

            public Color Color => Colors.WhiteSmoke;
            public Color BarColor
            {
                get
                {
                    if (UsedCapacity == 0)
                    {
                        return Capacity == 0
                            ? Colors.WhiteSmoke
                            : Colors.White;
                    }

                    if (IsOverloaded)
                    {
                        return Capacity == 0
                            ? Colors.LightCoral
                            : Lerp(Colors.MistyRose, Colors.LightCoral, 0.5f);
                    }

                    return Lerp(Colors.Honeydew,
                        Colors.LightGreen,
                        (float)UsedCapacityPercent);
                }
            }

            public override string ToString()
            {
                return $"{UsedCapacityPercent * 100:n}% - {UsedCapacity:n} / {Capacity:n}";
            }
        }
        private class TaskModel
        {
            public int ID { get; set; }
            public BucketModel Bucket { get; set; }
            public double Consumption { get; set; }
            public object Group { get; set; }
            public Color Color { get; set; }
            public Color BorderColor { get; set; }
            public Color FontColor { get; set; }
            public string Text { get; set; }

            public override string ToString() => Text;
        }

        #endregion
    }
}
