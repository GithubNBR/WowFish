using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoScript;
using KenerlImport;

namespace WowScript
{
    public class CastScript : IAutoScript
    {

        private volatile bool _isRunning;

        private const int FishingKey = 0x31; // 按键数字1,此键是Windows虚拟按钮，与AscII码不同

        public string Name => "自动施法";

        public bool IsRunning => _isRunning;

        public object ScriptData => throw new NotImplementedException();

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (_isRunning) return;

            _isRunning = true;
            while (_isRunning)
            {
                User32.keybd_event((byte)FishingKey, 0, InputEventConstant.KeyEventKeyDown, 0);
                await Task.Delay(50);
                User32.keybd_event((byte)FishingKey, 0, InputEventConstant.KeyEventKeyUp, 0);

                await Task.Delay(700);  // GCD
            }
        }

        public async Task StopAsync()
        {
            await Task.Yield();
            _isRunning = false;
        }
    }
}
