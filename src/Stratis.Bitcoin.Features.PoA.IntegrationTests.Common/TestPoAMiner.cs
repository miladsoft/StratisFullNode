﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.AsyncWork;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Validators;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.PoA.Voting;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.PoA.IntegrationTests.Common
{
    public class TestPoAMiner : PoAMiner
    {
        private readonly EditableTimeProvider timeProvider;

        private readonly CancellationTokenSource cancellation;

        private readonly ISlotsManager slotsManager;

        public TestPoAMiner(
            IConsensusManager consensusManager,
            IDateTimeProvider dateTimeProvider,
            Network network,
            INodeLifetime nodeLifetime,
            ILoggerFactory loggerFactory,
            IInitialBlockDownloadState ibdState,
            BlockDefinition blockDefinition,
            ISlotsManager slotsManager,
            IConnectionManager connectionManager,
            PoABlockHeaderValidator poaHeaderValidator,
            IFederationManager federationManager,
            IFederationHistory federationHistory,
            IIntegrityValidator integrityValidator,
            IWalletManager walletManager,
            INodeStats nodeStats,
            VotingManager votingManager,
            PoASettings poAMinerSettings,
            IAsyncProvider asyncProvider,
            IIdleFederationMembersKicker idleFederationMembersKicker,
            NodeSettings nodeSettings)
            : base(consensusManager, dateTimeProvider, network, nodeLifetime, ibdState, blockDefinition, slotsManager, connectionManager,
                poaHeaderValidator, federationManager, federationHistory, integrityValidator, walletManager, nodeStats, votingManager, poAMinerSettings, asyncProvider, idleFederationMembersKicker, nodeSettings)
        {
            this.timeProvider = dateTimeProvider as EditableTimeProvider;

            this.cancellation = new CancellationTokenSource();
            this.slotsManager = slotsManager;
        }

        public override void InitializeMining()
        {
        }

        public async Task MineBlocksAsync(int count)
        {
            for (int i = 0; i < count; i++)
            {
                this.timeProvider.AdjustedTimeOffset += this.slotsManager.GetRoundLength(this.federationManager.GetFederationMembers().Count);

                uint timeNow = (uint)this.timeProvider.GetAdjustedTimeAsUnixTimestamp();

                uint myTimestamp = this.slotsManager.GetMiningTimestamp(timeNow);

                this.timeProvider.AdjustedTimeOffset += TimeSpan.FromSeconds(myTimestamp - timeNow);

                ChainedHeader chainedHeader = await this.MineBlockAtTimestampAsync(myTimestamp).ConfigureAwait(false);

                if (chainedHeader == null)
                {
                    i--;
                    this.timeProvider.AdjustedTimeOffset += TimeSpan.FromHours(1);
                    continue;
                }

                var builder = new StringBuilder();
                builder.AppendLine("<<==============================================================>>");
                builder.AppendLine($"Block was mined {chainedHeader}.");
                builder.AppendLine("<<==============================================================>>");
                this.logger.LogInformation(builder.ToString());
            }
        }

        public override void Dispose()
        {
            this.cancellation.Cancel();
            base.Dispose();
        }
    }
}
