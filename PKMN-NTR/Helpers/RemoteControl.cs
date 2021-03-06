﻿using System;
using System.Threading.Tasks;
using System.Timers;

namespace ntrbase.Helpers
{
    class RemoteControl
    {
        // Class variables
        private int maxtimeout = 5000; // Max timeout in ms
        public uint lastRead = 0; // Last read from RAM
        public int pid = 0;
        PKHeX validator = new PKHeX();
        private Timer NTRtimer;
        private bool timeout = false;

        // Offsets for remote controls
        private uint buttonsOff = 0x10df20;
        private uint touchscrOff = 0x10df24;
        private uint stickOff = 0x10df28;
        private int hid_pid = 0x10;
        public const int BOXSIZE = 30;
        public const int POKEBYTES = 232;

        // Class constructor
        public RemoteControl()
        {
            NTRtimer = new Timer(maxtimeout);
            NTRtimer.AutoReset = false;
            NTRtimer.Elapsed += NTRtimer_Tick;
            NTRtimer.Enabled = false;
        }

        // Log Handler
        private void WriteLastLog(string str)
        {
            Program.gCmdWindow.lastlog = str;
        }

        private bool CompareLastLog(string str)
        {
            return Program.gCmdWindow.lastlog.Contains(str);
        }

        private void Report(string log)
        {
            Program.gCmdWindow.BeginInvoke(Program.gCmdWindow.delAddLog, log);
        }

        // Button Handler
        public async Task<bool> waitbutton(uint key)
        {
            Report("NTR: Send button command 0x" + key.ToString("X3"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(key);
            Program.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free buttons");
                quickbuton(LookupTable.nokey, 250);
                return false;
            }
            else
            { // Free the buttons
                buttonByte = BitConverter.GetBytes(LookupTable.nokey);
                Program.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Button release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Button command sent correctly");
                    return true;
                }
            }
        }

        public async void quickbuton(uint key, int time)
        {
            Report("NTR: Send button command 0x" + key.ToString("X3") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(key);
            Program.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.nokey);
            Program.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            Report("NTR: Button command sent, no feedback provided");
        }

        public async Task<bool> waitSoftReset()
        {
            Report("NTR: Send soft-reset command 0xCF7");
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(0xCF7);
            Program.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished") || CompareLastLog("patching smdh"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free buttons");
                quickbuton(LookupTable.nokey, 250);
                return false;
            }
            else
            { // Free the buttons
                buttonByte = BitConverter.GetBytes(LookupTable.nokey);
                Program.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
                    if (CompareLastLog("finished") || CompareLastLog("patching smdh"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Button release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Soft-reset command sent correctly");
                    return true;
                }
            }
        }

        // Touch Screen Handler
        public async Task<bool> waittouch(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free the touchscreen");
                freetouch();
                return false;
            }
            else
            {  // Free the touch screen
                buttonByte = BitConverter.GetBytes(LookupTable.notouch);
                Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Touch screen release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Touch screen command sent correctly");
                    return true;
                }
            }
        }

        public async Task<bool> waitholdtouch(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen and hold at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed");
                return false;
            }
            else // Return sucess
            {
                NTRtimer.Stop();
                Report("NTR: Touch screen command sent correctly");
                return true;
            }
        }

        public async Task<bool> waitfreetouch()
        {
            Report("NTR: Free the touch screen");
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.notouch);
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed");
                return false;
            }
            else // Return sucess
            {
                NTRtimer.Stop();
                Report("NTR: Touch screen command sent correctly");
                return true;
            }
        }

        public async void quicktouch(decimal Xcoord, decimal Ycoord, int time)
        {
            Report("NTR: Touch the screen at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.notouch);
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        public async void holdtouch(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen and hold at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(100);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        public async void freetouch()
        {
            Report("NTR: Free the touch screen");
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.notouch);
            Program.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(100);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        private uint gethexcoord(decimal Xvalue, decimal Yvalue)
        {
            uint hexX = Convert.ToUInt32(Math.Round(Xvalue * 0xFFF / 319));
            uint hexY = Convert.ToUInt32(Math.Round(Yvalue * 0xFFF / 239));
            return 0x01000000 + hexY * 0x1000 + hexX;
        }

        // Control Stick Handler
        public async Task<bool> waitsitck(int Xvalue, int Yvalue)
        {
            Report("NTR: Move Control Stick to " + Xvalue.ToString("D3") + "," + Yvalue.ToString("D3"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            Program.scriptHelper.write(stickOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Control stick command failed, try to release it");
                quickstick(0, 0, 250);
                freetouch();
                return false;
            }
            else
            { // Free the control stick
                buttonByte = BitConverter.GetBytes(LookupTable.nostick);
                Program.scriptHelper.write(stickOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Control Stick release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Control Stick command sent correctly");
                    return true;
                }
            }
        }

        public async void quickstick(int Xvalue, int Yvalue, int time)
        {
            Report("NTR: Move Control Stick to " + Xvalue.ToString("D3") + "," + Yvalue.ToString("D3") + " during" + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            Program.scriptHelper.write(stickOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.nostick);
            Program.scriptHelper.write(stickOff, buttonByte, hid_pid);
            Report("NTR: Control Stick command sent, no feedback provided");
        }

        private uint getstickhex(int Xvalue, int Yvalue)
        {
            uint hexX = Convert.ToUInt32((Xvalue + 100) * 0xFFF / 200);
            uint hexY = Convert.ToUInt32((Yvalue + 100) * 0xFFF / 200);
            if (hexX >= 0x1000) hexX = 0xFFF;
            if (hexY >= 0x1000) hexY = 0xFFF;
            return 0x01000000 + hexY * 0x1000 + hexX;
        }

        // Memory Read Handler
        private void handleMemoryRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            lastRead = BitConverter.ToUInt32(args.data, 0);
            Program.gCmdWindow.isreading = false;
            Program.gCmdWindow.HandleRAMread(lastRead);
        }

        public async Task<bool> waitNTRread(uint address)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8"));
            Program.gCmdWindow.readoperation = true;
            lastRead = 0;
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], handleMemoryRead, null);
            Program.gCmdWindow.addwaitingForData(Program.scriptHelper.data(address, 0x04, pid), myArgs);
            setTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100);
                if (!Program.gCmdWindow.isreading)
                {
                    break;
                }
            }
            if (Program.gCmdWindow.isreading || timeout)
            {
                Report("NTR: Read failed");
                Program.gCmdWindow.readoperation = false;
                return false;
            }
            else
            {
                Program.gCmdWindow.readoperation = false;
                return true;
            }
        }

        private void handlePokeRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            validator.Data = PKHeX.decryptArray(args.data);
            Program.gCmdWindow.isreading = false;
        }

        public async Task<long> waitPokeRead(int box, int slot)
        {
            try
            {
                Report("NTR: Read pokémon data at box " + (box + 1) + ", slot " + (slot + 1));
                Program.gCmdWindow.readoperation = true;
                // Get offset
                uint dumpOff = Program.gCmdWindow.boxOff + (Convert.ToUInt32(box * BOXSIZE + slot) * POKEBYTES);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], handlePokeRead, null);
                Program.gCmdWindow.updateDumpBoxes(box, slot);
                Program.gCmdWindow.addwaitingForData(Program.scriptHelper.data(dumpOff, POKEBYTES, pid), myArgs);
                setTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100);
                    if (!Program.gCmdWindow.isreading)
                    {
                        break;
                    }
                }
                if (Program.gCmdWindow.isreading || timeout)
                {
                    Report("NTR: Read failed");
                    Program.gCmdWindow.readoperation = false;
                    return -2;
                }
                else if (validator.Species > 0 && validator.Species <= Program.gCmdWindow.MAXSPECIES)
                {
                    NTRtimer.Stop();
                    lastRead = (uint)validator.Species;
                    Program.gCmdWindow.dumpedPKHeX.Data = validator.Data;
                    Program.gCmdWindow.updateTabs();
                    Report("NTR: Read sucessful - PID 0x" + validator.PID.ToString("X8"));
                    Program.gCmdWindow.readoperation = false;
                    return validator.PID;
                }
                else // Empty slot
                {
                    NTRtimer.Stop();
                    Report("NTR: Empty pokémon data");
                    Program.gCmdWindow.readoperation = false;
                    return -1;
                }
            }
            catch (Exception ex)
            {
                NTRtimer.Stop();
                Report("NTR: Read failed with exception:");
                Report(ex.Message);
                return -2; // No data received
            }
        }

        public async Task<long> waitPokeRead(uint offset)
        {
            try
            {
                Report("NTR: Read pokémon data at offset 0x" + offset.ToString("X8"));
                Program.gCmdWindow.readoperation = true;
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], handlePokeRead, null);
                Program.gCmdWindow.addwaitingForData(Program.scriptHelper.data(offset, POKEBYTES, pid), myArgs);
                setTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100);
                    if (!Program.gCmdWindow.isreading)
                    {
                        break;
                    }
                }
                if (Program.gCmdWindow.isreading || timeout)
                {
                    Report("NTR: Read failed");
                    Program.gCmdWindow.readoperation = false;
                    return -2;
                }
                else if (validator.Species > 0 && validator.Species <= Program.gCmdWindow.MAXSPECIES)
                {
                    NTRtimer.Stop();
                    lastRead = (uint)validator.Species;
                    Program.gCmdWindow.dumpedPKHeX.Data = validator.Data;
                    Program.gCmdWindow.updateTabs();
                    Report("NTR: Read sucessful - PID 0x" + validator.PID.ToString("X8"));
                    Program.gCmdWindow.readoperation = false;
                    return validator.PID;
                }
                else // Empty slot
                {
                    NTRtimer.Stop();
                    Report("NTR: Empty pokémon data");
                    Program.gCmdWindow.readoperation = false;
                    return -1;
                }
            }
            catch (Exception ex)
            {
                NTRtimer.Stop();
                Report("NTR: Read failed with exception:");
                Report(ex.Message);
                return -2; // No data received
            }
        }

        public async Task<long> waitPartyRead(uint partyOff, int slot)
        {
            try
            {
                Report("NTR: Read pokémon data at party slot " + (slot + 1));
                Program.gCmdWindow.readoperation = true;
                uint dumpOff = Program.gCmdWindow.partyOff + Convert.ToUInt32(slot * 484);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], handlePokeRead, null);
                Program.gCmdWindow.addwaitingForData(Program.scriptHelper.data(dumpOff, POKEBYTES, pid), myArgs);
                setTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100);
                    if (!Program.gCmdWindow.isreading)
                    {
                        break;
                    }
                }
                if (Program.gCmdWindow.isreading || timeout)
                {
                    Report("NTR: Read failed");
                    Program.gCmdWindow.readoperation = false;
                    return -2;
                }
                else if (validator.Species > 0 && validator.Species <= Program.gCmdWindow.MAXSPECIES)
                {
                    NTRtimer.Stop();
                    lastRead = (uint)validator.Species;
                    Program.gCmdWindow.dumpedPKHeX.Data = validator.Data;
                    Program.gCmdWindow.updateTabs();
                    Report("NTR: Read sucessful - PID 0x" + validator.PID.ToString("X8"));
                    Program.gCmdWindow.readoperation = false;
                    return validator.PID;
                }
                else // Empty slot
                {
                    NTRtimer.Stop();
                    Report("NTR: Empty pokémon data");
                    Program.gCmdWindow.readoperation = false;
                    return -1;
                }
            }
            catch (Exception ex)
            {
                NTRtimer.Stop();
                Report("NTR: Read failed with exception:");
                Report(ex.Message);
                return -2; // No data received
            }
        }

        public async Task<bool> memoryinrange(uint address, uint value, uint range)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8"));
            Report("NTR: Expected value 0x" + value.ToString("X8") + " to 0x" + (value + range - 1).ToString("X8"));
            Program.gCmdWindow.readoperation = true;
            lastRead = value + range;
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], handleMemoryRead, null);
            Program.gCmdWindow.addwaitingForData(Program.scriptHelper.data(address, 0x04, pid), myArgs);
            setTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100);
                if (!Program.gCmdWindow.isreading)
                {
                    break;
                }
            }
            if (!Program.gCmdWindow.isreading)
            { // Data received
                if (lastRead >= value && lastRead < value + range)
                {
                    NTRtimer.Stop();
                    Report("NTR: Value in range: YES");
                    Program.gCmdWindow.readoperation = false;
                    return true;
                }
                else
                {
                    Report("NTR: Value in range: NO");
                    Program.gCmdWindow.readoperation = false;
                    return false;
                }
            }
            else // No data received
            {
                Report("NTR: Read failed");
                Program.gCmdWindow.readoperation = false;
                return false;
            }
        }

        public async Task<bool> timememoryinrange(uint address, uint value, uint range, int tick, int maxtime)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8") + " during " + maxtime + " ms");
            Report("NTR: Expected value 0x" + value.ToString("X8") + " to 0x" + (value + range - 1).ToString("X8"));
            Program.gCmdWindow.readoperation = true;
            setTimer(maxtime);
            while (!timeout)
            { // Ask for data
                lastRead = value + range;
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], handleMemoryRead, null);
                Program.gCmdWindow.addwaitingForData(Program.scriptHelper.data(address, 0x04, pid), myArgs);
                // Wait for data
                while (!timeout)
                {
                    await Task.Delay(100);
                    if (!Program.gCmdWindow.isreading)
                    {
                        break;
                    }
                }
                if (!Program.gCmdWindow.isreading)
                { // Data received
                    if (lastRead >= value && lastRead < value + range)
                    {
                        NTRtimer.Stop();
                        Report("NTR: Value in range: YES");
                        Program.gCmdWindow.readoperation = false;
                        return true;
                    }
                    else
                    {
                        Report("NTR: Value in range: No");
                        await Task.Delay(tick);
                    }
                } // If no data received or not in range, try again
                else
                {
                    Report("NTR: Read failed, try again");
                }
            }
            Report("NTR: Read failed or outside of range");
            Program.gCmdWindow.readoperation = false;
            return false;
        }

        // Memory Write handler
        public async Task<bool> waitNTRwrite(uint address, uint data, int pid)
        {
            Report("NTR: Write value 0x" + data.ToString("X8") + " at address 0x" + address.ToString("X8"));
            byte[] command = BitConverter.GetBytes(data);
            Program.scriptHelper.write(address, command, pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (!timeout)
            {
                NTRtimer.Stop();
                Report("NTR: Write sucessful");
                return true;
            }
            else
            {
                Report("NTR: Write failed");
                return false;
            }
        }

        // Timer
        private void setTimer(int time)
        {
            WriteLastLog("");
            timeout = false;
            NTRtimer.Interval = time;
            NTRtimer.Start();
        }

        private void NTRtimer_Tick(object sender, ElapsedEventArgs e)
        {
            Report("NTR: Command timed out");
            timeout = true;
        }
    }
}
