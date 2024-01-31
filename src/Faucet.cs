using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Faucet;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Faucet;

public partial class Faucet : FaucetContainer.FaucetBase
{
    public override Empty Initialize(InitializeInput input)
    {
        var nativeSymbol = Context.Variables.NativeSymbol;
        State.OwnerMap[nativeSymbol] = input.Admin ?? Context.Sender;
        State.LimitAmountMap[nativeSymbol] = input.AmountLimit == 0 ? DefaultLimitAmount : input.AmountLimit;
        State.IntervalMinutesMap[nativeSymbol] =
            input.IntervalMinutes == 0 ? DefaultIntervalMinutes : input.IntervalMinutes;
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        return new Empty();
    }

    public override Empty TurnOn(TurnInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertSenderIsOwner(symbol);
        AssertFaucetIsOff(symbol);
        State.OffAtMap.Remove(symbol);
        State.OnAtMap[symbol] = Context.CurrentBlockTime;
        Context.Fire(new FaucetTurned
        {
            IsTurnedOn = true,
            Symbol = symbol
        });
        return new Empty();
    }

    public override Empty TurnOff(TurnInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertSenderIsOwner(symbol);
        AssertFaucetIsOn(symbol);
        State.OnAtMap.Remove(symbol);
        State.OffAtMap[symbol] = Context.CurrentBlockTime;
        Context.Fire(new FaucetTurned
        {
            IsTurnedOn = false,
            Symbol = symbol
        });
        return new Empty();
    }

    public override Empty NewFaucet(NewFaucetInput input)
    {
        AssertSenderIsAdmin();
        State.OwnerMap[input.Symbol] = input.Owner;
        State.LimitAmountMap[input.Symbol] = input.AmountLimit == 0 ? DefaultLimitAmount : input.AmountLimit;
        State.IntervalMinutesMap[input.Symbol] =
            input.IntervalMinutes == 0 ? DefaultIntervalMinutes : input.IntervalMinutes;
        Context.Fire(new FaucetCreated
        {
            Owner = input.Owner,
            Symbol = input.Symbol
        });
        return new Empty();
    }

    public override Empty Pour(PourInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertSenderIsOwner(symbol);
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = Context.Self,
            Symbol = symbol,
            Amount = input.Amount
        });
        Context.Fire(new Poured
        {
            Symbol = symbol,
            Amount = input.Amount
        });
        return new Empty();
    }

    public override Empty SetLimit(SetLimitInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertSenderIsOwner(symbol);
        if (input.AmountLimit != 0)
        {
            State.LimitAmountMap[symbol] = input.AmountLimit;
        }

        if (input.IntervalMinutes != 0)
        {
            State.IntervalMinutesMap[symbol] = input.IntervalMinutes;
        }

        Context.Fire(new LimitChanged
        {
            Symbol = symbol,
            LimitAmount = State.LimitAmountMap[symbol],
            IntervalMinutes = State.IntervalMinutesMap[symbol]
        });
        return new Empty();
    }

    public override Empty Ban(BanInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertSenderIsOwner(symbol);
        if (input.IsBan)
        {
            State.BanMap[symbol][input.Target] = true;
        }
        else
        {
            State.BanMap[symbol].Remove(input.Target);
        }

        Context.Fire(new Banned
        {
            Symbol = symbol,
            Target = input.Target,
            IsBanned = input.IsBan
        });

        return new Empty();
    }

    public override Empty Send(SendInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertSenderIsOwner(symbol);
        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = input.Target,
            Symbol = symbol,
            Amount = input.Amount
        });

        Context.Fire(new Sent
        {
            Symbol = symbol,
            Target = input.Target,
            Amount = input.Amount
        });
        return new Empty();
    }

    public override Empty Take(TakeInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        AssertFaucetIsOn(symbol);
        Assert(State.BanMap[symbol][Context.Sender] == false, $"Sender is banned by faucet owner of {symbol}");
        var latestTakeTime = State.LatestTakeTimeMap[symbol][Context.Sender];
        if (latestTakeTime != null)
        {
            var nextAvailableTime = latestTakeTime.AddMinutes(State.IntervalMinutesMap[symbol]);
            Assert(Context.CurrentBlockTime >= nextAvailableTime,
                $"Can take {symbol} again after {nextAvailableTime}");
        }

        var amount = Math.Min(State.LimitAmountMap[symbol], input.Amount);
        Assert(amount > 0,
            $"Cannot take {input.Amount} from {symbol} faucet due to either limit amount ({State.LimitAmountMap[symbol]}) or input amount ({input.Amount}) is negative or zero.");
        State.TokenContract.Transfer.Send(new TransferInput
        {
            Symbol = symbol,
            Amount = amount,
            To = Context.Sender
        });

        State.LatestTakeTimeMap[symbol][Context.Sender] = Context.CurrentBlockTime;

        Context.Fire(new Taken
        {
            Symbol = symbol,
            Amount = amount,
            User = Context.Sender
        });
        return new Empty();
    }

    public override Empty Return(ReturnInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        var amount = input.Amount;
        if (input.Amount == 0)
        {
            amount = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = Context.Sender,
                Symbol = symbol
            }).Balance;
        }

        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = Context.Self,
            Amount = amount,
            Symbol = symbol
        });

        Context.Fire(new Returned
        {
            Symbol = symbol,
            User = Context.Sender,
            Amount = amount
        });
        return new Empty();
    }

    public override Address GetOwner(StringValue input)
    {
        return State.OwnerMap[ReturnNativeSymbolIfEmpty(input.Value)];
    }

    public override FaucetStatus GetFaucetStatus(StringValue input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Value);

        var maybeOnAt = State.OnAtMap[symbol];
        var isOn = maybeOnAt != null;
        var status = new FaucetStatus
        {
            IsOn = isOn,
            TurnAt = isOn ? maybeOnAt : State.OffAtMap[symbol]
        };

        return status;
    }

    public override Int64Value GetLimitAmount(StringValue input)
    {
        return new Int64Value
        {
            Value = State.LimitAmountMap[input.Value]
        };
    }

    public override Int64Value GetIntervalMinutes(StringValue input)
    {
        return new Int64Value
        {
            Value = State.IntervalMinutesMap[input.Value]
        };
    }

    public override BoolValue IsBannedByOwner(IsBannedByOwnerInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
        return new BoolValue
        {
            Value = State.BanMap[symbol][input.Target]
        };
    }

    private void AssertSenderIsOwner(string symbol)
    {
        Assert(Context.Sender == State.OwnerMap[symbol], $"No permission to operate faucet of {symbol}.");
    }

    private void AssertSenderIsAdmin()
    {
        Assert(Context.Sender == State.OwnerMap[Context.Variables.NativeSymbol], "No permission.");
    }

    private void AssertFaucetIsOn(string symbol)
    {
        var offAt = State.OffAtMap[symbol];
        var onAt = State.OnAtMap[symbol];
        if (onAt == null && offAt == null)
        {
            throw new AssertionException($"Faucet of {symbol} never turned on.");
        }

        if (onAt == null)
        {
            throw new AssertionException($"Faucet of {symbol} is off.");
        }
    }

    private void AssertFaucetIsOff(string symbol)
    {
        var onAt = State.OnAtMap[symbol];
        Assert(onAt == null, $"Faucet of {symbol} is on.");
    }

    private string ReturnNativeSymbolIfEmpty(string symbol)
    {
        return string.IsNullOrEmpty(symbol) ? Context.Variables.NativeSymbol : symbol;
    }
}