using Microsoft.Toolkit.Mvvm.ComponentModel;
using SmwAceHelper.Utilities;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace SmwAceHelper.Models
{
    [JsonConverter(typeof(CustomEnumConverter<PlayerDirection>))]
    public enum PlayerDirection
    {
        Left,
        Right,
    }

    public class StrategyItem : ObservableObject, ICloneable
    {
        public static readonly short PLAYER_X_MIN = 0x0008;
        public static readonly short PLAYER_X_MAX = 0x13E8;
        public static readonly short PLAYER_Y_MIN = 0x0000;
        public static readonly short PLAYER_Y_MAX = 0x0190;

        private static readonly BitmapImage[] images = new BitmapImage[]
        {
            new BitmapImage(new Uri("/Resources/Mario_Left.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Left_Shell.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Left_Yoshi.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Left_Yoshi_Shell.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Right.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Right_Shell.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Right_Yoshi.png", UriKind.Relative)),
            new BitmapImage(new Uri("/Resources/Mario_Right_Yoshi_Shell.png", UriKind.Relative)),
        };

        private PlayerDirection direction;
        private short? playerX;
        private short? playerY;
        private bool rideOnYoshi;
        private bool haveShell;
        private string memo;

        // Difference between Player and Yoshi/Shell coordinates
        private int ShellX_PlayerX
        {
            get
            {
                int diff = RideOnYoshi ? 18 : 11;
                return (Direction == PlayerDirection.Left) ? -diff : diff;
            }
        }
        private int YoshiX_PlayerX { get { return (Direction == PlayerDirection.Left) ? -2 : 2; } }
        private int ShellY_PlayerY { get { return RideOnYoshi ? 16 : 15; } }
        private int YoshiY_PlayerY { get { return 16; } }

        private short? Add(short? value, int diff)
        {
            return value.HasValue ? (short)(value.Value + diff) : null;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public StrategyItem()
        {
            this.direction = PlayerDirection.Right;
            this.memo = string.Empty;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="item">Object to copy</param>
        public StrategyItem(StrategyItem item)
        {
            this.direction = item.direction;
            this.playerX = item.playerX;
            this.playerY = item.playerY;
            this.rideOnYoshi = item.rideOnYoshi;
            this.haveShell = item.HaveShell;
            this.memo = item.memo;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new StrategyItem(this);
        }

        /// <summary>
        /// Player direction
        /// </summary>
        public PlayerDirection Direction
        {
            get { return direction; }
            set
            {
                if (direction != value)
                {
                    Model.Current.History.OnPropertyChanging(this, direction, value);
                    OnPropertyChanging(nameof(YoshiX));
                    OnPropertyChanging(nameof(ShellX));
                    OnPropertyChanging(nameof(Image));
                    SetProperty(ref direction, value);
                    OnPropertyChanged(nameof(YoshiX));
                    OnPropertyChanged(nameof(ShellX));
                    OnPropertyChanged(nameof(Image));
                }
            }
        }

        /// <summary>
        /// Player X position
        /// </summary>
        public short? PlayerX
        {
            get { return playerX; }
            set
            {
                if (value.HasValue)
                {
                    value = Math.Min(Math.Max(PLAYER_X_MIN, value.Value), PLAYER_X_MAX);
                }
                if (playerX != value)
                {
                    Model.Current.History.OnPropertyChanging(this, playerX, value);
                    OnPropertyChanging(nameof(YoshiX));
                    OnPropertyChanging(nameof(ShellX));
                    SetProperty(ref playerX, value);
                    OnPropertyChanged(nameof(YoshiX));
                    OnPropertyChanged(nameof(ShellX));
                }
            }
        }

        /// <summary>
        /// Player Y position
        /// </summary>
        public short? PlayerY
        {
            get { return playerY; }
            set
            {
                if (value.HasValue)
                {
                    value = Math.Min(Math.Max(PLAYER_Y_MIN, value.Value), PLAYER_Y_MAX);
                }
                if (playerY != value)
                {
                    Model.Current.History.OnPropertyChanging(this, playerY, value);
                    OnPropertyChanging(nameof(YoshiY));
                    OnPropertyChanging(nameof(ShellY));
                    SetProperty(ref playerY, value);
                    OnPropertyChanged(nameof(YoshiY));
                    OnPropertyChanged(nameof(ShellY));
                }
            }
        }

        /// <summary>
        /// Whether player is riding on Yoshi
        /// </summary>
        public bool RideOnYoshi
        {
            get { return rideOnYoshi; }
            set
            {
                if (rideOnYoshi != value)
                {
                    Model.Current.History.OnPropertyChanging(this, rideOnYoshi, value);
                    OnPropertyChanging(nameof(ShellX));
                    OnPropertyChanging(nameof(Image));
                    SetProperty(ref rideOnYoshi, value);
                    OnPropertyChanged(nameof(ShellX));
                    OnPropertyChanged(nameof(Image));
                }
            }
        }

        /// <summary>
        /// Yoshi X position
        /// </summary>
        [JsonIgnore]
        public short? YoshiX
        {
            get { return Add(PlayerX, YoshiX_PlayerX); }
            set { PlayerX = Add(value, -YoshiX_PlayerX); }
        }

        /// <summary>
        /// Yoshi Y position
        /// </summary>
        [JsonIgnore]
        public short? YoshiY
        {
            get { return Add(PlayerY, YoshiY_PlayerY); }
            set { PlayerY = Add(value, -YoshiY_PlayerY); }
        }

        /// <summary>
        /// Whether player or Yoshi has a shell
        /// </summary>
        public bool HaveShell
        {
            get { return haveShell; }
            set
            {
                if (haveShell != value)
                {
                    Model.Current.History.OnPropertyChanging(this, haveShell, value);
                    OnPropertyChanging(nameof(Image));
                    SetProperty(ref haveShell, value);
                    OnPropertyChanged(nameof(Image));
                }
            }
        }

        /// <summary>
        /// Shell X position
        /// </summary>
        /// <remarks>
        /// If Yoshi has a shell in his mouth, this value is the coordinate after spit out the flame.
        /// </remarks>
        [JsonIgnore]
        public short? ShellX
        {
            get { return Add(PlayerX, ShellX_PlayerX); }
            set { PlayerX = Add(value, -ShellX_PlayerX); }
        }

        /// <summary>
        /// Shell Y position
        /// </summary>
        /// <remarks>
        /// If Yoshi has a shell in his mouth, this value is the coordinate after spit out the flame.
        /// </remarks>
        [JsonIgnore]
        public short? ShellY
        {
            get { return Add(PlayerY, ShellY_PlayerY); }
            set { PlayerY = Add(value, -ShellY_PlayerY); }
        }

        /// <summary>
        /// Memo
        /// </summary>
        public string Memo
        {
            get { return memo; }
            set
            {
                Model.Current.History.OnPropertyChanging(this, memo, value);
                SetProperty(ref memo, value);
            }
        }

        /// <summary>
        /// Image
        /// </summary>
        [JsonIgnore]
        public BitmapImage? Image
        {
            get
            {
                int index = ((int)Direction << 2) | (RideOnYoshi ? 2 : 0) | (HaveShell ? 1 : 0);
                return images.ElementAtOrDefault(index);
            }
        }

        /// <summary>
        /// Check if the value can be set.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Value</param>
        /// <returns>Whether the value can be set or not.</returns>
        public bool CanSetValue(string propertyName, object? value)
        {
            switch (propertyName)
            {
                case nameof(Direction):
                    if (value is PlayerDirection dir)
                    {
                        return Enum.IsDefined<PlayerDirection>(dir);
                    }
                    break;

                case nameof(PlayerX):
                    return InRange(value, PLAYER_X_MIN, PLAYER_X_MAX);

                case nameof(PlayerY):
                    return InRange(value, PLAYER_Y_MIN, PLAYER_Y_MAX);

                case nameof(YoshiX):
                    return InRange(value, PLAYER_X_MIN + YoshiX_PlayerX, PLAYER_X_MAX + YoshiX_PlayerX);

                case nameof(YoshiY):
                    return InRange(value, PLAYER_Y_MIN + YoshiY_PlayerY, PLAYER_Y_MAX + YoshiY_PlayerY);

                case nameof(ShellX):
                    return InRange(value, PLAYER_X_MIN + ShellX_PlayerX, PLAYER_X_MAX + ShellX_PlayerX);

                case nameof(ShellY):
                    return InRange(value, PLAYER_Y_MIN + ShellY_PlayerY, PLAYER_Y_MAX + ShellY_PlayerY);

                case nameof(RideOnYoshi):
                case nameof(HaveShell):
                    return value is bool;

                case nameof(Memo):
                    return value is string;
            }
            return false;
        }

        /// <summary>
        /// Check if the value is within the range.
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="minValue">Minimum value</param>
        /// <param name="maxValue">Maximum value</param>
        /// <returns>Whether the value is within the range.</returns>
        private bool InRange(object? value, int minValue, int maxValue)
        {
            if (value == null)
            {
                return true;
            }
            else if (value is short shortValue)
            {
                return (minValue <= shortValue) && (shortValue <= maxValue);
            }
            else
            {
                return false;
            }
        }
    }
}
