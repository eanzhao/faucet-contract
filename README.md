# Faucet Contract for aelf

This repo is a refactor of https://github.com/AElfProject/aelf-faucet-contract via new aelf contract code generator and the github action to deploy contract to aelf testnet.

# Feature

For faucet owner:
- **NewFaucet**: Init a new faucet with any tokens on aelf network. Of course the token should be created on the MultiToken Contract.
- **Pour**: Add tokens to a faucet.
- **TurnOn** / **TurnOn**: Turn on / turn off a faucet.
- **SetLimit**: Set limit of the amount each time a user can take from the faucet.
- **Ban**: Ban an account from taking tokens from faucet.
- **Send**: Send tokens directly to an account, and the tokens come from the faucet.

For faucet user:
- **Take**: Take tokens from faucet.
- **Return**: Return tokens to faucet.