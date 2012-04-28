namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// The way a game finishes
    /// </summary>
    public enum FinishType
    {
        /// <summary>
        /// Game is running (will execute next instruction)
        /// </summary>
        Running,

        /// <summary>
        /// Game should quit after this instruction
        /// </summary>
        Quit,

        /// <summary>
        /// Game should restart after this instruction
        /// </summary>
        Restart,
    }
}
