using GolemClientMockAPI.Entities;
using GolemClientMockAPI.Processors;
using GolemClientMockAPI.Repository;
using GolemMarketApiMockup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GolemMarketApiMockupTests
{
    [TestClass]
    public class MarketIntegrationTests
    {
        public ISubscriptionRepository SubscriptionRepository { get; set; }
        public IProposalRepository ProposalRepository { get; set; }
        public IAgreementRepository AgreementRepository { get; set; }

        public IRequestorMarketProcessor RequestorProcessor { get; set; }
        public IProviderMarketProcessor ProviderProcessor { get; set; }

        [TestInitialize]
        public void InitializeTests()
        {
            var subsPropRepo = new InProcessSubscriptionProposalRepository();

            this.SubscriptionRepository = subsPropRepo;
            this.ProposalRepository = subsPropRepo;
            this.AgreementRepository = new InProcessAgreementRepository();

            var marketProcessor = new InMemoryMarketProcessor(
                this.SubscriptionRepository, 
                this.ProposalRepository, 
                this.AgreementRepository);

            this.RequestorProcessor = marketProcessor;
            this.ProviderProcessor = marketProcessor;
        }

        /// <summary>
        /// Perform the matching part of the Market API basic scenario.
        /// </summary>
        /// <returns></returns>
        public async Task<(DemandSubscription, OfferSubscription, OfferProposal)> DoDefaultDemandOfferMatching()
        {
            // Subscribe Demand

            var demand = new Demand()
            {
                Constraints = @"(&" +
                    @"(golem.srv.comp.container.docker.image=golemfactory/ffmpeg)" +
                    @"(golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}>300)" +
                    @"(golem.com.payment.scheme=after)" +
                    @"(golem.usage.vector=[golem.usage.duration_sec])" +
                    @"(golem.com.pricing.model=linear)" +
                //@"(golem.com.pricing.est{[30]}<125)" +
                @")",
                NodeId = "RequestorA", // must be assigned from the outside
                Properties = new Dictionary<string, JToken>()
            };

            var demandSubscription = RequestorProcessor.SubscribeDemand(demand);


            Assert.IsNotNull(demandSubscription.Id);
            Assert.IsNotNull(demandSubscription.Demand);
            Assert.IsNull(demandSubscription.LastReceivedProposalId);

            // Subscribe Offers

            var offer1 = new Offer()
            {
                Constraints = "()",
                NodeId = "ProviderA",
                Properties = new Dictionary<string, JToken>()
                {
                    { "golem.srv.comp.container.docker.image", new JArray(@"golemfactory/ffmpeg") },
                    { "golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}", 7},
                    { "golem.srv.comp.container.docker.benchmark{*}", null},
                    { "golem.inf.cpu.cores", 4},
                    { "golem.inf.cpu.threads", 8},
                    { "golem.inf.mem.gib", 16},
                    { "golem.inf.storage.gib", 30},
                    { "golem.usage.vector", new JArray(@"golem.usage.duration_sec")},
                    { "golem.com.payment.scheme", "after"},
                    { "golem.com.pricing.model", "linear"},
                    { "golem.com.pricing.est{*}", null},
                }
            };

            var offer2 = new Offer()
            {
                Constraints = "()",
                NodeId = "ProviderB",
                Properties = new Dictionary<string, JToken>()
                {
                    { "golem.srv.comp.container.docker.image", new JArray(@"golemfactory/ffmpeg") },
                    { "golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}", 723},
                    { "golem.srv.comp.container.docker.benchmark{*}", null},
                    { "golem.inf.cpu.cores", 4},
                    { "golem.inf.cpu.threads", 8},
                    { "golem.inf.mem.gib", 16},
                    { "golem.inf.storage.gib", 30},
                    { "golem.usage.vector", new JArray(@"golem.usage.duration_sec")},
                    { "golem.com.payment.scheme", @"after"},
                    { "golem.com.pricing.model", @"linear"},
                    { "golem.com.pricing.est{*}", null},
                }
            };

            var offer1Subscription = ProviderProcessor.SubscribeOffer(offer1);
            var offer2Subscription = ProviderProcessor.SubscribeOffer(offer2);

            Assert.IsNotNull(offer1Subscription.Id);
            Assert.IsNotNull(offer1Subscription.Offer);
            Assert.IsNull(offer1Subscription.LastReceivedProposalId);

            Assert.IsNotNull(offer2Subscription.Id);
            Assert.IsNotNull(offer2Subscription.Offer);
            Assert.IsNull(offer2Subscription.LastReceivedProposalId);


            // ...at this point in scenario, the Collect on Requestor side should return an offer already...

            var reqEvents = await RequestorProcessor.CollectRequestorEventsAsync(demandSubscription.Id, 1.000f, 10);

            var offerProposal = reqEvents.First();

            Assert.IsTrue(reqEvents.Any());

            // note that these should not be equal 
            // (SubscriptionId as perceived by Provider is different than Offer Proposal Id as seen by the Requestor)
            Assert.AreNotEqual(offer2Subscription.Id, offerProposal.OfferProposal.Id);

            return (demandSubscription, offer2Subscription, offerProposal.OfferProposal);

        }

        public async Task<OfferProposal> DoSimpleOfferNegotiation(DemandSubscription demandSubscription, OfferSubscription offerSubscription, OfferProposal offerProposal)
        {
            // OK, we got an offer proposal, we can now send a direct Demand counterproposal

            var counterDemand = new Demand()
            {
                Constraints = @"(&" +
                    @"(golem.srv.comp.container.docker.image=golemfactory/ffmpeg)" +
                    @"(golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}>300)" +
                    @"(golem.com.payment.scheme=after)" +
                    @"(golem.usage.vector=[golem.usage.duration_sec])" +
                    @"(golem.com.pricing.model=linear)" +
                    @"(golem.com.pricing.model.linear.coeffs=*)" +
                //@"(golem.com.pricing.est{[30]}<125)" +
                @")",
                NodeId = "RequestorA", // must be assigned from the outside
                Properties = new Dictionary<string, JToken>()
            };

            var counterDemandProposal = RequestorProcessor.CreateDemandProposal(demandSubscription.Id, offerProposal.Id, counterDemand);

            // At this point, the Provider should be able to read the Demand Proposal on the Offer subscription

            var provEvents = await ProviderProcessor.CollectProviderEventsAsync(offerSubscription.Id, 1.000f, 10);

            Assert.IsTrue(provEvents.Any());
            Assert.AreEqual(counterDemandProposal.Id, provEvents.First().DemandProposal.Id);
            Assert.AreEqual(offerProposal.Id, provEvents.First().DemandProposal.OfferId);

            // ...if we call again - there should be no proposals, so a timeout is expected after 100ms
            var provEvents2 = await ProviderProcessor.CollectProviderEventsAsync(offerSubscription.Id, 0.100f, 10);

            Assert.IsFalse(provEvents2.Any());

            // Now test the blocking logic of Collect - hang on Collect on Requestor side, while Provider sends counter-offer

            ICollection<MarketRequestorEvent> reqEvents2 = null;
            Task.Run(async () =>
            {
                reqEvents2 = await RequestorProcessor.CollectRequestorEventsAsync(demandSubscription.Id, 5.000f, 10);

                Assert.IsTrue(reqEvents2.Any());
            });

            // ...and prepare counter-offer with price

            var counterOffer = new Offer()
            {
                Constraints = "()",
                NodeId = "ProviderB",
                Properties = new Dictionary<string, JToken>()
                {
                    { "golem.srv.comp.container.docker.image", @"[""golemfactory/ffmpeg""]" },
                    { "golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}", @"723"},
                    { "golem.srv.comp.container.docker.benchmark{*}", null},
                    { "golem.inf.cpu.cores", @"4"},
                    { "golem.inf.cpu.threads", @"8"},
                    { "golem.inf.mem.gib", @"16"},
                    { "golem.inf.storage.gib", @"30"},
                    { "golem.usage.vector", @"[""golem.usage.duration_sec""]"},
                    { "golem.com.payment.scheme", @"""after"""},
                    { "golem.com.pricing.model", @"""linear"""},
                    { "golem.com.pricing.model.linear.coeffs", @"[0, 0.2]"},
                    { "golem.com.pricing.est{*}", null},
                }
            };

            var counterOfferProposal = ProviderProcessor.CreateOfferProposal(offerSubscription.Id, provEvents.First().DemandProposal.Id, counterOffer);

            Thread.Sleep(1000);

            Assert.IsTrue(reqEvents2.Any());
            Assert.AreEqual(counterDemandProposal.Id, reqEvents2.First().OfferProposal.DemandId);

            return reqEvents2.First().OfferProposal;
        }

        public void DoSubscriptionCleanup(DemandSubscription demandSubscription, OfferSubscription offerSubscription)
        {
            this.RequestorProcessor.UnsubscribeDemand(demandSubscription.Id);
            this.ProviderProcessor.UnsubscribeOffer(offerSubscription.Id);
        }

        [TestMethod]
        public async Task InMemoryMarketResolver_Integration_SimplePositiveScenario()
        {
            // Test scenario

            // Subscribe Demand

            var (demandSubscription, offerSubscription, offerProposal) = await this.DoDefaultDemandOfferMatching();

            // OK, we got an offer proposal, let's haggle a bit...

            var lastOfferProposal = await this.DoSimpleOfferNegotiation(demandSubscription, offerSubscription, offerProposal);

            // OK, so the reqEvents2 now contains an offer which is acceptable.
            // Requestor can move on to propose Agreement...

            var agreement = RequestorProcessor.CreateAgreement(lastOfferProposal.Id, System.DateTime.Now);

            Assert.IsNotNull(agreement);
            Assert.AreEqual(lastOfferProposal.Id, agreement.Id);
            Assert.AreEqual(AgreementState.Proposal, agreement.State);
            Assert.IsNotNull(agreement.DemandProposal);

            // Confirm and start waiting for Agreement proposal response
            AgreementResultEnum? agreementResponse = null;
            Task.Run(async () =>
            {
                agreementResponse = await RequestorProcessor.ConfirmAgreementAsync(agreement.Id, 5.000f);

                Assert.IsNotNull(agreementResponse);
            });


            // ...in the meantime - collect the Agreement proposal on Provider side and send response

            var provEvents3 = await ProviderProcessor.CollectProviderEventsAsync(offerSubscription.Id, 100.000f, 10);

            Assert.IsTrue(provEvents3.Any());
            var receivedAgreementProposal = provEvents3.First();

            Assert.AreEqual(MarketProviderEvent.MarketProviderEventType.AgreementProposal, receivedAgreementProposal.EventType);

            ProviderProcessor.ApproveAgreement(receivedAgreementProposal.Agreement.Id);

            Thread.Sleep(1000);

            Assert.AreEqual(AgreementResultEnum.Approved, agreementResponse);



            // run another collect on Provider side - allegedly this returns a Proposal Event again???

            var provEvents4 = await ProviderProcessor.CollectProviderEventsAsync(offerSubscription.Id, 5.000f, 10);

            Assert.IsFalse(provEvents4.Any());

            DoSubscriptionCleanup(demandSubscription, offerSubscription);

        }

        [TestMethod]
        public async Task InMemoryMarketResolver_Integration_AgreementRejectedScenario()
        {
            // Test scenario

            // Subscribe Demand

            var (demandSubscription, offerSubscription, offerProposal) = await this.DoDefaultDemandOfferMatching();

            // OK, we got an offer proposal, let's haggle a bit...

            var lastOfferProposal = await this.DoSimpleOfferNegotiation(demandSubscription, offerSubscription, offerProposal);

            // OK, so the reqEvents2 now contains an offer which is acceptable.
            // Requestor can move on to propose Agreement...

            var agreement = RequestorProcessor.CreateAgreement(lastOfferProposal.Id, System.DateTime.Now);

            Assert.IsNotNull(agreement);
            Assert.AreEqual(lastOfferProposal.Id, agreement.Id);
            Assert.AreEqual(AgreementState.Proposal, agreement.State);
            Assert.IsNotNull(agreement.DemandProposal);

            // Confirm and start waiting for Agreement proposal response
            AgreementResultEnum? agreementResponse = null;
            Task.Run(async () =>
            {
                agreementResponse = await RequestorProcessor.ConfirmAgreementAsync(agreement.Id, 5.000f);

                Assert.IsNotNull(agreementResponse);
            });


            // ...in the meantime - collect the Agreement proposal on Provider side and send response

            var provEvents3 = await ProviderProcessor.CollectProviderEventsAsync(offerSubscription.Id, 100.000f, 10);

            Assert.IsTrue(provEvents3.Any());
            var receivedAgreementProposal = provEvents3.First();

            Assert.AreEqual(MarketProviderEvent.MarketProviderEventType.AgreementProposal, receivedAgreementProposal.EventType);

            ProviderProcessor.RejectAgreement(receivedAgreementProposal.Agreement.Id);

            Thread.Sleep(1000);

            Assert.AreEqual(AgreementResultEnum.Rejected, agreementResponse);

            DoSubscriptionCleanup(demandSubscription, offerSubscription);

        }

        [TestMethod]
        public async Task InMemoryMarketResolver_Integration_AgreementCancelledScenario()
        {
            // Test scenario

            // Subscribe Demand

            var (demandSubscription, offerSubscription, offerProposal) = await this.DoDefaultDemandOfferMatching();

            // OK, we got an offer proposal, let's haggle a bit...

            var lastOfferProposal = await this.DoSimpleOfferNegotiation(demandSubscription, offerSubscription, offerProposal);

            // OK, so the reqEvents2 now contains an offer which is acceptable.
            // Requestor can move on to propose Agreement...

            var agreement = RequestorProcessor.CreateAgreement(lastOfferProposal.Id, System.DateTime.Now);

            Assert.IsNotNull(agreement);
            Assert.AreEqual(lastOfferProposal.Id, agreement.Id);
            Assert.AreEqual(AgreementState.Proposal, agreement.State);
            Assert.IsNotNull(agreement.DemandProposal);

            // Confirm and start waiting for Agreement proposal response
            AgreementResultEnum? agreementResponse = null;
            Task.Run(async () =>
            {
                agreementResponse = await RequestorProcessor.ConfirmAgreementAsync(agreement.Id, 5.000f);

                Assert.IsNotNull(agreementResponse);
            });

            Thread.Sleep(500); // wait a little to allow the Confirm to be sent, and then...

            RequestorProcessor.CancelAgreement(agreement.Id);

            // ...in the meantime - collect the Agreement proposal on Provider side and send response

            var provEvents3 = await ProviderProcessor.CollectProviderEventsAsync(offerSubscription.Id, 100.000f, 10);

            // TODO in Cancel implementation...
            // If Agreement Proposal hasn't been yet collected - simply remove it from pipeline
            // otherwise - block the agreement response pipeline with some cancel token
            // so that attempt to Approve ends with Agreement cancelled error...


            Assert.IsTrue(provEvents3.Any());
            var receivedAgreementProposal = provEvents3.First();

            Assert.AreEqual(MarketProviderEvent.MarketProviderEventType.AgreementProposal, receivedAgreementProposal.EventType);

            ProviderProcessor.ApproveAgreement(receivedAgreementProposal.Agreement.Id);

            Assert.AreEqual(AgreementState.Cancelled, agreement.State);

            Thread.Sleep(1000);

            Assert.AreEqual(AgreementResultEnum.Cancelled, agreementResponse);

            DoSubscriptionCleanup(demandSubscription, offerSubscription);
        }


    }
}
