using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace ChangeSleepMode
{
    public partial class MainWindow : Window
    {
        SYSTEM_POWER_POLICY spp;

        // Массив для перечисления доступных значений времени отключения дисплея и перехода в спящий режим (в минутах)
        readonly int[] Times = new int[16]
        {
            1,
            2,
            3,
            5,
            10,
            15,
            20,
            25,
            30,
            45,
            60,
            120,
            180,
            240,
            300,
            0
        };


        public MainWindow()
        {
            InitializeComponent();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetCurrentTimes();
        }

        // Автоматически устанавливает полученные значений времен в интерфейс программы
        void SetCurrentTimes()
        {
            uint retval = NativeMethods.CallNtPowerInformation(
                NativeMethods.SystemPowerPolicyDc,
                IntPtr.Zero,
                0,
                out spp,
                Marshal.SizeOf(typeof(SYSTEM_POWER_POLICY))
            );

            if (retval == NativeMethods.STATUS_SUCCESS)
            {
                for (int i = 0; i < Times.Length; i++)
                {
                    if (Times[i] == TimeSpan.FromSeconds(spp.VideoTimeout).TotalMinutes)
                    {
                        CbDisplayFromBattery.SelectedIndex = i;
                    }
                    if (Times[i] == TimeSpan.FromSeconds(spp.IdleTimeout).TotalMinutes)
                    {
                        CbSleepFromBattery.SelectedIndex = i;
                    }
                }
            }

            retval = NativeMethods.CallNtPowerInformation(
                NativeMethods.SystemPowerPolicyAc,
                IntPtr.Zero,
                0,
                out spp,
                Marshal.SizeOf(typeof(SYSTEM_POWER_POLICY))
            );

            if (retval == NativeMethods.STATUS_SUCCESS)
            {
                for (int i = 0; i < Times.Length; i++)
                {
                    if (Times[i] == TimeSpan.FromSeconds(spp.VideoTimeout).TotalMinutes)
                    {
                        CbDisplayFromPower.SelectedIndex = i;
                    }
                    if (Times[i] == TimeSpan.FromSeconds(spp.IdleTimeout).TotalMinutes)
                    {
                        CbSleepFromPower.SelectedIndex = i;
                    }
                }
            }
        }


        // Устанавливает значение Combobox'ов по умолчанию
        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            CbDisplayFromBattery.SelectedIndex = 3;
            CbDisplayFromPower.SelectedIndex = 4;
            CbSleepFromBattery.SelectedIndex = 5;
            CbSleepFromPower.SelectedIndex = 8;
        }

        void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessCmd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ProcessPowerShell();
            }
        }

        // Сохраняет текущие изменения через Cmd
        private void ProcessCmd()
        {
            string[] allVariants = new string[4]
            {
                $"/c powercfg /change monitor-timeout-ac {Times[CbDisplayFromPower.SelectedIndex]}",
                $"/c powercfg /change monitor-timeout-dc {Times[CbDisplayFromBattery.SelectedIndex]}",
                $"/c powercfg /change standby-timeout-ac {Times[CbSleepFromPower.SelectedIndex]}",
                $"/c powercfg /change standby-timeout-dc {Times[CbSleepFromBattery.SelectedIndex]}"
            };

            for (int i = 0; i < allVariants.Length; i++)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = allVariants[i],
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
        }

        // Сохраняет текущие изменения через PowerShell
        private void ProcessPowerShell()
        {
            string[] allVariants = new string[4]
            {
                $"/command powercfg /change monitor-timeout-ac {Times[CbDisplayFromPower.SelectedIndex]}",
                $"/command powercfg /change monitor-timeout-dc {Times[CbDisplayFromBattery.SelectedIndex]}",
                $"/command powercfg /change standby-timeout-ac {Times[CbSleepFromPower.SelectedIndex]}",
                $"/command powercfg /change standby-timeout-dc {Times[CbSleepFromBattery.SelectedIndex]}"
            };

            for (int i = 0; i < allVariants.Length; i++)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = allVariants[i],
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Что-то для помощи получения значений времени отключения дисплея и перехода в спящий режим
    static class NativeMethods
    {
        public const int SystemPowerPolicyAc = 0;
        public const int SystemPowerPolicyDc = 1;

        public const uint STATUS_SUCCESS = 0;

        [DllImport("powrprof.dll")]
        public static extern uint CallNtPowerInformation(
            int InformationLevel,
            IntPtr InputBuffer,
            int InputBufferLength,
            out SYSTEM_POWER_POLICY spi,
            int OutputBufferLength
        );
    }

    public enum POWER_ACTION : uint
    {
        PowerActionNone = 0,      // No system power action.
        PowerActionReserved,      // Reserved; do not use.
        PowerActionSleep,         // Sleep.
        PowerActionHibernate,     // Hibernate.
        PowerActionShutdown,      // Shutdown.
        PowerActionShutdownReset, // Shutdown and reset.
        PowerActionShutdownOff,   // Shutdown and power off.
        PowerActionWarmEject,     // Warm eject.
    }

    [Flags]
    public enum PowerActionFlags : uint
    {
        POWER_ACTION_QUERY_ALLOWED = 0x00000001,  // Broadcasts a PBT_APMQUERYSUSPEND event to each application to request permission to suspend operation.
        POWER_ACTION_UI_ALLOWED = 0x00000002,     // Applications can prompt the user for directions on how to prepare for suspension. Sets bit 0 in the Flags parameter passed in the lParam parameter of WM_POWERBROADCAST.
        POWER_ACTION_OVERRIDE_APPS = 0x00000004,  // Ignores applications that do not respond to the PBT_APMQUERYSUSPEND event broadcast in the WM_POWERBROADCAST message.
        POWER_ACTION_LIGHTEST_FIRST = 0x10000000, // Uses the first lightest available sleep state.
        POWER_ACTION_LOCK_CONSOLE = 0x20000000,   // Requires entry of the system password upon resume from one of the system standby states.
        POWER_ACTION_DISABLE_WAKES = 0x40000000,  // Disables all wake events.
        POWER_ACTION_CRITICAL = 0x80000000,       // Forces a critical suspension.
    }

    [Flags]
    public enum PowerActionEventCode : uint
    {
        POWER_LEVEL_USER_NOTIFY_TEXT = 0x00000001,  // User notified using the UI.
        POWER_LEVEL_USER_NOTIFY_SOUND = 0x00000002, // User notified using sound.
        POWER_LEVEL_USER_NOTIFY_EXEC = 0x00000004,  // Specifies a program to be executed.
        POWER_USER_NOTIFY_BUTTON = 0x00000008,      // Indicates that the power action is in response to a user power button press.
        POWER_USER_NOTIFY_SHUTDOWN = 0x00000010,    // Indicates a power action of shutdown/off.
        POWER_FORCE_TRIGGER_RESET = 0x80000000,     // Clears a user power button press.
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct POWER_ACTION_POLICY
    {
        public POWER_ACTION Action;
        public PowerActionFlags Flags;
        public PowerActionEventCode EventCode;
    }

    public enum SYSTEM_POWER_STATE : UInt32
    {
        PowerSystemUnspecified = 0,
        PowerSystemWorking = 1,
        PowerSystemSleeping1 = 2,
        PowerSystemSleeping2 = 3,
        PowerSystemSleeping3 = 4,
        PowerSystemHibernate = 5,
        PowerSystemShutdown = 6,
        PowerSystemMaximum = 7
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SYSTEM_POWER_LEVEL // SIZE MUST BE 24 bytes
    {
        public byte Enable;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Spare;
        public uint BatteryLevel;
        public POWER_ACTION_POLICY PowerPolicy;
        public SYSTEM_POWER_STATE MinSystemState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SYSTEM_POWER_POLICY // SIZE MUST BE 232 bytes
    {
        public uint Revision;
        public POWER_ACTION_POLICY PowerButton;
        public POWER_ACTION_POLICY SleepButton;
        public POWER_ACTION_POLICY LidClose;
        public SYSTEM_POWER_STATE LidOpenWake;
        public uint Reserved;
        public POWER_ACTION_POLICY Idle;
        public uint IdleTimeout;
        public byte IdleSensitivity;
        public byte DynamicThrottle;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Spare2;
        public SYSTEM_POWER_STATE MinSleep;
        public SYSTEM_POWER_STATE MaxSleep;
        public SYSTEM_POWER_STATE ReducedLatencySleep;
        public uint WinLogonFlags;
        public uint Spare3;
        public uint DozeS4Timeout;
        public uint BroadcastCapacityResolution;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public SYSTEM_POWER_LEVEL[] DischargePolicy;
        public uint VideoTimeout;
        public byte VideoDimDisplay;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] VideoReserved;
        public uint SpindownTimeout;
        public byte OptimizeForPower;
        public byte FanThrottleTolerance;
        public byte ForcedThrottle;
        public byte MinThrottle;
        public POWER_ACTION_POLICY OverThrottled;
    }
}
