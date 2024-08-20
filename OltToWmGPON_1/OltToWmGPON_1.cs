/*
****************************************************************************
*  Copyright (c) 2021,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

/*
 ****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

08/07/2020	1.0.0.1		DPR, Skyline	Initial Version
20/08/2024	1.0.0.3		DPR, Skyline	Update NuGet interapp

 *****************************************************************************
 *
 */

#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
using Skyline.DataMiner.Core.InterAppCalls.Common.Shared;
using Skyline.Protocol.EpmApi;

public class Script
{
	public static void Run(Engine engine)
	{
		try
		{
			engine.Timeout = new TimeSpan(0, 15, 0);

			string epmMessage = engine.GetScriptParam("Message").Value;
			TopicMessagingRoot mess = JsonConvert.DeserializeObject<TopicMessagingRoot>(epmMessage);

			string[] split = mess.Data.RequesterId.Split('_');

			if (split.Length != 2)
			{
				return;
			}

			int iDmaId = Convert.ToInt32(split[0]);

			IInterAppCall command = InterAppCallFactory.CreateNew();
			command.Messages.Add(mess);
			command.Source = new Source("OltToWmGPON");
			List<Element> wm = engine.FindElementsByProtocol("Skyline EPM Platform GPON WM").Where(x => (x.IsActive && x.ProtocolVersion == "Production" && x.DmaId == iDmaId)).ToList();
			if (wm.Count != 1)
			{
				engine.DebugMessage("ERROR|Run|No Skyline EPM Platform GPON WM element was found.");
				return;
			}

			command.ReturnAddress = new ReturnAddress(wm[0].DmaId, wm[0].ElementId, 9000001);
			command.Send(Engine.SLNetRaw, wm[0].DmaId, wm[0].ElementId, 9000000, new List<Type> { typeof(TopicMessagingRoot), typeof(TopicDataInfo) });
		}
		catch (Exception e)
		{
			engine.DebugMessage("ERROR|Run|Exception: " + e);
		}
	}
}

public static class Extensions
{
	public static void DebugMessage(this Engine engine, string message)
	{
#if DEBUG
		engine.Log(message);
#endif
	}
}

namespace Skyline.Protocol
{
	namespace EpmApi
	{
		public class TopicMessagingRoot : Message
		{
			[JsonProperty("workflow")]
			public string Workflow { get; set; }

			[JsonProperty("topic")]
			public string TopicName { get; set; }

			[JsonProperty("messageType")]
			public string MessageType { get; set; }

			[JsonProperty("data")]
			public TopicDataInfo Data { get; set; }
		}

		public class TopicDataInfo
		{
			[JsonProperty("requesterId")]
			public string RequesterId { get; set; }

			[JsonProperty("requesterName")]
			public string RequesterName { get; set; }

			[JsonProperty("responseTime")]
			public string ResponseTime { get; set; }

			[JsonProperty("requestDateTime")]
			public string RequestDateTime { get; set; }

			[JsonProperty("lastTimestamp")]
			public string LastTimestamp { get; set; }
		}
	}
}