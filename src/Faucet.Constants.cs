namespace AElf.Contracts.Faucet;

public partial class Faucet
{
    private const long DefaultLimitAmount = 10_00000000;
    private const long DefaultIntervalMinutes = long.MaxValue; // Which means one account can only take tokens from the faucet one time.
}