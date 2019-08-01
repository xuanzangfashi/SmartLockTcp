using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace LinuxTcpServerDotnetCore.SmartLock.Statics
{
    public enum ETcpHandlerType
    {
        EApp, ESmartLock,
    }

    public class FSmartLockPair
    {
        public TcpConnectionHandler_SmartLock sl;
        public TcpConnectionHandler_App app;
        public FSmartLockPair(bool a = false)
        {
            sl = null;
            app = null;
        }
    }

    public enum EHttpCommitMsgType
    {
        EFactorChange,
    }

    public enum EDataHeader
    {
        EInit, EGpsTrigger, EGeofencingTrigger, EPhoneBluetoothDetected,
        EBluetoothTagDetected, EDeviceDetected, EHumanCountImg,
        EFingerprintData, EFaceData, EPinInput, EVoiceInput, ECommitFactor, EExit,
    }

    public enum EFactorState
    {
        EUndetected, ESuccess, EFail, EUnSelected,
    }

    public enum EFactorName
    {
        EGPS, EGeofencing, EPhoneBluetooth,
        EBluetoothTag, EDeviceId, EHumanCount,
        EFingerprint, EFaceId, EPin, EVoice,
    }

    public enum ELightState
    {
        EGreen = 1, ERed, EOrange, EGreenBlink = 101, ERedBlink, EOrangeBlink,
    }

    public struct FLockInfo
    {
        public string id;
        public string lock_id;
        public string username;
        public string[] phone_id;
        public string[] bluetooth_id;
        public string[] device_id;
        public string[] fingerprint_data;
        public string[] face_data;
        public string pin;
        public int[][] selected_factor;
        public Vector3 lock_location;
        public EFactorState[][] factor_state;
        public EFactorState[] resident_factor_state;//1.Pin 2.fingerprint 3.face_id -------------  index base on 1
        public EFactorState multiple_human;
    }


    public static class LightManager
    {
        //public static ELightState GetLightState(FLockInfo lockinfo,int makeup_count)
        //{
        //    int fail_count = 0;
        //    foreach (var i in lockinfo.factor_state)
        //    {
        //        if (i == EFactorState.EFail || i == EFactorState.EUndetected)
        //        {
        //            fail_count++;
        //        }
        //    }
        //    fail_count = Math.Clamp(fail_count, 0, 2);
        //    fail_count -= makeup_count;
        //    if (fail_count >= 2)
        //    {
        //        return ELightState.ERed;
        //    }
        //    else if (fail_count == 1)
        //    {
        //        return ELightState.EOrange;
        //    }
        //    else
        //    {
        //        return ELightState.EGreen;
        //    }
        //}
    }
}
