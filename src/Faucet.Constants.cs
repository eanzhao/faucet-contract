namespace AElf.Contracts.Faucet;

public partial class Faucet
{
    private const long DefaultLimitAmount = 10_00000000;
    private const long DefaultIntervalMinutes = long.MaxValue; // On account can only take tokens once
}