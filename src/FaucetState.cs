using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Faucet;

public class FaucetState : ContractState
{
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

    public MappedState<string, Address> OwnerMap { get; set; }
    public MappedState<string, Timestamp> OnAtMap { get; set; }
    public MappedState<string, Timestamp> OffAtMap { get; set; }
    public MappedState<string, long> LimitAmountMap { get; set; }
    public MappedState<string, long> IntervalMinutesMap { get; set; }
    public MappedState<string, Address, bool> BanMap { get; set; }

    /// <summary>
    /// Symbol -> Take Address -> Latest Take Time.
    /// </summary>
    public MappedState<string, Address, Timestamp> LatestTakeTimeMap { get; set; }
}