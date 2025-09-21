namespace WalkSimulatorClassic
{
    public static class Sounds
    { 
        public static void Play(int sound, float volume = 0.08f) => GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(sound, isLeftHand: false, volume);
    }
}
