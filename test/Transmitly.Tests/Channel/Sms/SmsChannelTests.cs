﻿// ﻿﻿Copyright (c) Code Impressions, LLC. All Rights Reserved.
//  
//  Licensed under the Apache License, Version 2.0 (the "License")
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using Moq;
using AutoFixture;
using Transmitly.Exceptions;
using Transmitly.Tests;

namespace Transmitly.Channel.Sms.Tests
{
	[TestClass()]
	public class SmsChannelTests : BaseUnitTest
	{

		[TestMethod()]
		[DataRow("+14155552671", true)]
		[DataRow("+442071838750", true)]
		[DataRow("+551155256325", true)]
		[DataRow("551155256325", true)]
		[DataRow("51155256325", true)]
		[DataRow("(511)55256325", false)]
		[DataRow("511-552-56325", false)]
		[DataRow("+1 511-552-56325", false)]
		[DataRow("+1 51155256325", false)]
		[DataRow("+37060112345", true)]
		[DataRow("+64010", true)]//NZ service number
		[DataRow("+1231234567890", true)]
		[DataRow("+2902124", true)]
		[DataRow("15551231234", true)]
		public void SupportsIdentityAddressTest(string value, bool expected)
		{
			var sms = new SmsChannel();
			var result = sms.SupportsIdentityAddress(value.AsIdentityAddress());
			Assert.AreEqual(expected, result, value);
		}

		[TestMethod]
		public async Task GenerateCommunicationAsyncShouldGuardAgainstNullContext()
		{
			var channel = fixture.Create<SmsChannel>();

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => channel.GenerateCommunicationAsync(null!));
		}

		[TestMethod]
		public async Task GenerateCommunicationAsyncShouldGenerateValidSmsCommunication()
		{
			var mockContext = fixture.Create<Mock<IDispatchCommunicationContext>>();
			mockContext.Setup(x => x.ContentModel!.Resources).Returns([]);
			var context = mockContext.Object;
			var sut = fixture.Create<SmsChannel>();
			var body = fixture.Freeze<string>();
			sut.Message.AddStringTemplate(body);

			var result = await sut.GenerateCommunicationAsync(context);

			Assert.IsInstanceOfType(result, typeof(ISms));
			var sms = (ISms)result;
			Assert.AreEqual(sut.From, sms.From);
			Assert.AreEqual(body, sms.Message);
			Assert.AreEqual(context.TransportPriority, sms.TransportPriority);
			Assert.AreEqual(sut.DeliveryReportCallbackUrl, sms.DeliveryReportCallbackUrl);
			Assert.AreEqual(sut.DeliveryReportCallbackUrlResolver, sms.DeliveryReportCallbackUrlResolver);
			CollectionAssert.AreEquivalent(mockContext.Object.PlatformIdentities.SelectMany(m => m.Addresses).ToArray(), sms.To);
		}

		[TestMethod]
		public void ShouldSetProvidedChannelProviderIds()
		{
			var list = fixture.Freeze<string[]>();
			var sut = new SmsChannel(list);
			CollectionAssert.AreEquivalent(list, sut.AllowedChannelProviderIds.ToArray());
		}

		[TestMethod]
		public void GeneratingCommunicationShouldThrowWithoutMessageTemplate()
		{
			var mockContext = fixture.Create<Mock<IDispatchCommunicationContext>>();
			mockContext.Setup(x => x.ContentModel!.Resources).Returns([]);
			var context = mockContext.Object;
			var sut = fixture.Create<SmsChannel>();
			var body = fixture.Freeze<string>();

			Assert.ThrowsExceptionAsync<CommunicationsException>(() => sut.GenerateCommunicationAsync(context));
		}

		[TestMethod]
		public void ShouldSetProvidedFromAddress()
		{
			var from = fixture.Freeze<IIdentityAddress>();
			var sut = new SmsChannel(from);
			Assert.AreSame(from, sut.From);

			sut = new SmsChannel(from, fixture.Create<string[]>());
			Assert.AreSame(from, sut.From);
		}

		[TestMethod]
		public async Task ShouldSetProvidedFromAddressResolver()
		{
			var from = fixture.Freeze<IIdentityAddress>();
			var mockContext = fixture.Create<Mock<IDispatchCommunicationContext>>();
			mockContext.Setup(x => x.ContentModel!.Resources).Returns([]);
			var context = mockContext.Object;
			var body = fixture.Freeze<string>();


			var sut = new SmsChannel((ctx) => from);
			sut.Message.AddStringTemplate(body);
			var result = await sut.GenerateCommunicationAsync(context);
			var sms = (ISms)result;
			Assert.AreSame(from, sms.From);
		}

		[TestMethod]
		public async Task ContentModelResourceShouldAddSmsAttachment()
		{
			var from = fixture.Freeze<IIdentityAddress>();
			var mockContext = fixture.Create<Mock<IDispatchCommunicationContext>>();
			var resource = new Resource("res", "ct", new MemoryStream());
			mockContext.Setup(x => x.ContentModel!.Resources).Returns([resource]);
			var context = mockContext.Object;
			var body = fixture.Freeze<string>();
			var sut = new SmsChannel();
			sut.Message.AddStringTemplate(body);

			var result = await sut.GenerateCommunicationAsync(context);
			var sms = (ISms)result;
			Assert.AreEqual(1, sms.Attachments.Count);
			Assert.AreEqual(resource.Name, sms.Attachments.First().Name);
		}
	}
}