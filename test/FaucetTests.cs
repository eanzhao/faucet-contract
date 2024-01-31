using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Faucet;

public class FaucetTests : TestBase
{
    [Fact]
    public async Task PipelineTest()
    {
        var keyPair = SampleAccount.Accounts[0].KeyPair;
        var adminStub = GetFaucetContractStub(keyPair);
        var adminTokenStub = GetTokenContractStub(keyPair);
        var userStub = GetFaucetContractStub(SampleAccount.Accounts.Skip(1).First().KeyPair);
        var userTokenStub = GetTokenContractStub(SampleAccount.Accounts.Skip(1).First().KeyPair);

        await adminStub.Initialize.SendAsync(new InitializeInput());

        // Check faucet status.
        {
            var faucetStatus = await adminStub.GetFaucetStatus.CallAsync(new StringValue
            {
                Value = "ELF"
            });
            faucetStatus.IsOn.ShouldBeFalse();
            faucetStatus.TurnAt.ShouldBeNull();
        }

        // User failed to take.
        {
            var executionResult = await userStub.Take.SendWithExceptionAsync(new TakeInput
            {
                Symbol = "ELF",
                Amount = 100_00000000
            });
            executionResult.TransactionResult.Error.ShouldContain("never turned on.");
        }

        // Turn on.
        await adminStub.TurnOn.SendAsync(new TurnInput());

        // Check owner.
        var owner = await adminStub.GetOwner.CallAsync(new StringValue
        {
            Value = "ELF"
        });
        owner.ShouldBe(SampleAccount.Accounts.First().Address);

        // Check faucet status.
        {
            var faucetStatus = await adminStub.GetFaucetStatus.CallAsync(new StringValue
            {
                Value = "ELF"
            });
            faucetStatus.IsOn.ShouldBeTrue();
            faucetStatus.TurnAt.ShouldNotBeNull();
        }

        // Pour.
        await adminTokenStub.Approve.SendAsync(new ApproveInput
        {
            Spender = ContractAddress,
            Symbol = "ELF",
            Amount = long.MaxValue
        });
        await adminStub.Pour.SendAsync(new PourInput
        {
            Amount = 1000_00000000
        });

        // Check faucet status.
        {
            var faucetStatus = await adminStub.GetFaucetStatus.CallAsync(new StringValue
            {
                Value = "ELF"
            });
            faucetStatus.IsOn.ShouldBeTrue();
            faucetStatus.TurnAt.ShouldNotBeNull();
        }

        // User takes.
        {
            await userStub.Take.SendAsync(new TakeInput
            {
                Symbol = "ELF",
                Amount = 100_00000000
            });
            // Check user balance.
            var balance = (await adminTokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = SampleAccount.Accounts.Skip(1).First().Address,
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe(100_00000000);
        }

        // User returns.
        {
            await userTokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = ContractAddress,
                Symbol = "ELF",
                Amount = long.MaxValue
            });
            await userStub.Return.SendAsync(new ReturnInput
            {
                Symbol = "ELF"
            });
            // Check user balance.
            var balance = (await adminTokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = SampleAccount.Accounts.Skip(1).First().Address,
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe(0);
        }

        // Cannot turn on twice.
        {
            var executionResult = await adminStub.TurnOn.SendWithExceptionAsync(new TurnInput());
            executionResult.TransactionResult.Error.ShouldContain("is on");
        }

        // Turn off.
        await adminStub.TurnOff.SendAsync(new TurnInput());

        // Check faucet status.
        {
            var faucetStatus = await adminStub.GetFaucetStatus.CallAsync(new StringValue
            {
                Value = "ELF"
            });
            faucetStatus.IsOn.ShouldBeFalse();
            faucetStatus.TurnAt.ShouldNotBeNull();
        }

        // User should be failed to take again.
        {
            var executionResult = await userStub.Take.SendWithExceptionAsync(new TakeInput
            {
                Symbol = "ELF",
                Amount = 100_00000000
            });
            executionResult.TransactionResult.Error.ShouldContain("is off");
        }

        // Turn on again.
        await adminStub.TurnOn.SendAsync(new TurnInput());

        // Check faucet status.
        {
            var faucetStatus = await adminStub.GetFaucetStatus.CallAsync(new StringValue
            {
                Value = "ELF"
            });
            faucetStatus.IsOn.ShouldBeTrue();
            faucetStatus.TurnAt.ShouldNotBeNull();
        }

        // Turn off again.
        await adminStub.TurnOff.SendAsync(new TurnInput());

        // Check faucet status.
        {
            var faucetStatus = await adminStub.GetFaucetStatus.CallAsync(new StringValue
            {
                Value = "ELF"
            });
            faucetStatus.IsOn.ShouldBeFalse();
            faucetStatus.TurnAt.ShouldNotBeNull();
        }
    }
}