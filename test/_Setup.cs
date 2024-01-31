using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace AElf.Contracts.Faucet;

// The Module class load the context required for unit testing
public class Module : ContractTestModule<Faucet>
{
        
}
    
// The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
public class TestBase : ContractTestBase<Module>
{
    // The Stub class for unit testing
    internal readonly FaucetContainer.FaucetStub FaucetStub;

    // A key pair that can be used to interact with the contract instance
    private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

    public TestBase()
    {
        FaucetStub = GetFaucetContractStub(DefaultKeyPair);
    }

    internal FaucetContainer.FaucetStub GetFaucetContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<FaucetContainer.FaucetStub>(ContractAddress, senderKeyPair);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
    }
}