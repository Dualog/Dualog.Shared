﻿using System;
using System.Collections.Generic;
using System.Text;
using Dualog.Shared.Enums;
using Dualog.Shared.Extensions;
using Dualog.Shared.Models;

namespace Dualog.Shared.Messages
{
    public class TRAMessage : Message
    {
        public DateTime ReloadDateTime { get; }
        public ReloadingPurpose ReloadingPurpose { get; }
        public string Latitude { get; }
        public string Longitude { get; }
        public IReadOnlyList<FishFAOAndWeight> FishOnBoard { get; }
        public IReadOnlyList<FishFAOAndWeight> TransferedFish { get; }
        public string RadioCallSignalForOtherParty { get; }

        public TRAMessage(
            DateTime sent, 
            ReloadingPurpose reloadingPurpose, 
            string latitude, 
            string longitude,
            DateTime reloadDateTime, 
            IReadOnlyList<FishFAOAndWeight> fishOnBoard, 
            IReadOnlyList<FishFAOAndWeight> transferedFish, 
            string radioCallSignalForOtherParty,
            string skipperName,
            Ship ship,
            string cancelCode = "") : base(MessageType.TRA, sent, skipperName, ship, errorCode:cancelCode)
        {
            this.ReloadingPurpose = reloadingPurpose;
            Latitude = latitude;
            Longitude = longitude;
            ReloadDateTime = reloadDateTime;
            FishOnBoard = fishOnBoard;
            TransferedFish = transferedFish;
            RadioCallSignalForOtherParty = radioCallSignalForOtherParty;
        }

        protected override void WriteBody(StringBuilder sb)
        {
            if (MessageFieldChecker.ZoneUsesFormat.LtLg(ForwardTo))
            {
                sb.Append($"//LT/{Latitude}");
                sb.Append($"//LG/{Longitude}");
            }
            else if (MessageFieldChecker.ZoneUsesFormat.LaLo(ForwardTo))
            {
                sb.Append($"//LA/{Latitude}");
                sb.Append($"//LO/{Longitude}");
            }
            sb.Append($"//OB/{FishOnBoard.ToNAF()}");
            sb.Append($"//KG/{TransferedFish.ToNAF()}");
            sb.Append(ReloadingPurpose == ReloadingPurpose.Receiving
                ? $"//TF/{RadioCallSignalForOtherParty.ToUpper().Trim()}"
                : $"//TT/{RadioCallSignalForOtherParty.ToUpper().Trim()}");
            sb.Append($"//PD/{ReloadDateTime.ToFormattedDate()}");
            sb.Append($"//PT/{ReloadDateTime.ToFormattedTime()}");
        }

        public static TRAMessage ParseNAFFormat(int id, DateTime sent, IReadOnlyDictionary<string, string> values)
        {
            var lat = "";
            var lon = "";
            if (values.ContainsKey("LA"))
            {
                lat = values["LA"];
            }
            if (values.ContainsKey("LO"))
            {
                lon = values["LO"];
            }
            if (values.ContainsKey("LT"))
            {
                lat = values["LT"];
            }
            if (values.ContainsKey("LG"))
            {
                lon = values["LG"];
            }
            //TT is set if the boat is delivering to another boat
            ReloadingPurpose purpose = values.ContainsKey("TT")
                ? ReloadingPurpose.Delivering
                : ReloadingPurpose.Receiving;
            return new TRAMessage(
                sent, 
                purpose,
                lat,
                lon,
                (values["PD"] + values["PT"]).FromFormattedDateTime(),
                MessageParsing.ParseFishWeights(values["OB"]),
                MessageParsing.ParseFishWeights(values["KG"]),
                purpose == ReloadingPurpose.Receiving ? values["TF"] : values["TT"],
                values["MA"], 
                new Ship(values["NA"], values["RC"], values["XR"]),
                values.ContainsKey("RE") ? values["RE"] : string.Empty)
            {
                Id = id,
                ForwardTo = values.ContainsKey("FT") ? values["FT"] : string.Empty,
                SequenceNumber = values.ContainsKey("SQ") ? Convert.ToInt32(values["SQ"]) : 0
            };
        }
    }
}