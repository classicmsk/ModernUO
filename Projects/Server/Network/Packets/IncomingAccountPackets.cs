/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingAccountPackets.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using CV = Server.ClientVersion;

namespace Server.Network
{
    public static class IncomingAccountPackets
    {
        private const int m_AuthIDWindowSize = 128;
        private static readonly Dictionary<int, AuthIDPersistence> m_AuthIDWindow =
            new(m_AuthIDWindowSize);

        internal struct AuthIDPersistence
        {
            public DateTime Age;
            public readonly ClientVersion Version;

            public AuthIDPersistence(ClientVersion v)
            {
                Age = DateTime.UtcNow;
                Version = v;
            }
        }

        public static void Configure()
        {
            IncomingPackets.Register(0x00, 104, false, CreateCharacter);
            IncomingPackets.Register(0x5D, 73, false, PlayCharacter);
            IncomingPackets.Register(0x80, 62, false, AccountLogin);
            IncomingPackets.Register(0x83, 39, false, DeleteCharacter);
            IncomingPackets.Register(0x91, 65, false, GameLogin);
            IncomingPackets.Register(0xA0, 3, false, PlayServer);
            IncomingPackets.Register(0xBB, 9, false, AccountID);
            IncomingPackets.Register(0xBD, 0, false, ClientVersion);
            IncomingPackets.Register(0xBE, 0, true, AssistVersion);
            IncomingPackets.Register(0xCF, 0, false, AccountLogin);
            IncomingPackets.Register(0xE1, 0, false, ClientType);
            IncomingPackets.Register(0xEF, 21, false, LoginServerSeed);
            IncomingPackets.Register(0xF8, 106, false, CreateCharacter);
        }

        public static void CreateCharacter(NetState state, CircularBufferReader reader)
        {
            var unk1 = reader.ReadInt32();
            var unk2 = reader.ReadInt32();
            int unk3 = reader.ReadByte();
            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);
            var flags = reader.ReadInt32();
            reader.Seek(8, SeekOrigin.Current);
            int prof = reader.ReadByte();
            reader.Seek(15, SeekOrigin.Current);

            int genderRace = reader.ReadByte();

            int str = reader.ReadByte();
            int dex = reader.ReadByte();
            int intl = reader.ReadByte();
            int is1 = reader.ReadByte();
            int vs1 = reader.ReadByte();
            int is2 = reader.ReadByte();
            int vs2 = reader.ReadByte();
            int is3 = reader.ReadByte();
            int vs3 = reader.ReadByte();

            SkillNameValue[] skills;

            // We have protocol changes by now, so this is ok
            if (state.NewCharacterCreation)
            {
                int is4 = reader.ReadByte();
                int vs4 = reader.ReadByte();

                skills = new[]
                {
                    new SkillNameValue((SkillName)is1, vs1),
                    new SkillNameValue((SkillName)is2, vs2),
                    new SkillNameValue((SkillName)is3, vs3),
                    new SkillNameValue((SkillName)is4, vs4)
                };
            }
            else
            {
                skills = new[]
                {
                    new SkillNameValue((SkillName)is1, vs1),
                    new SkillNameValue((SkillName)is2, vs2),
                    new SkillNameValue((SkillName)is3, vs3)
                };
            }

            int hue = reader.ReadUInt16();
            int hairVal = reader.ReadInt16();
            int hairHue = reader.ReadInt16();
            int hairValf = reader.ReadInt16();
            int hairHuef = reader.ReadInt16();
            reader.ReadByte();
            int cityIndex = reader.ReadByte();
            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();
            int shirtHue = reader.ReadInt16();
            int pantsHue = reader.ReadInt16();

            /*
            Pre-7.0.0.0:
            0x00, 0x01 -> Human Male, Human Female
            0x02, 0x03 -> Elf Male, Elf Female

            Post-7.0.0.0:
            0x00, 0x01
            0x02, 0x03 -> Human Male, Human Female
            0x04, 0x05 -> Elf Male, Elf Female
            0x05, 0x06 -> Gargoyle Male, Gargoyle Female
            */

            var female = genderRace % 2 != 0;

            var raceID = state.StygianAbyss ? (byte)(genderRace < 4 ? 0 : genderRace / 2 - 1) : (byte)(genderRace / 2);
            Race race = Race.Races[raceID] ?? Race.DefaultRace;

            var info = state.CityInfo;
            var a = state.Account;

            if (info == null || a == null || cityIndex < 0 || cityIndex >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal)
                    {
                        state.WriteConsole("Account in use");
                        state.SendPopupMessage(PMMessage.CharInWorld);
                        return;
                    }
                }

                state.Flags = (ClientFlags)flags;

                var args = new CharacterCreatedEventArgs(
                    state,
                    a,
                    name,
                    female,
                    hue,
                    str,
                    dex,
                    intl,
                    info[cityIndex],
                    skills,
                    shirtHue,
                    pantsHue,
                    hairVal,
                    hairHue,
                    hairValf,
                    hairHuef,
                    prof,
                    race
                );

                state.SendClientVersionRequest();

                state.BlockAllPackets = true;

                EventSink.InvokeCharacterCreated(args);

                var m = args.Mobile;

                if (m != null)
                {
                    state.Mobile = m;
                    m.NetState = state;
                    new LoginTimer(state, m).Start();
                }
                else
                {
                    state.BlockAllPackets = false;
                    state.Dispose();
                }
            }
        }

        public static void DeleteCharacter(NetState state, CircularBufferReader reader)
        {
            reader.Seek(30, SeekOrigin.Current);
            var index = reader.ReadInt32();

            EventSink.InvokeDeleteRequest(state, index);
        }

        public static void AccountID(NetState state, CircularBufferReader reader)
        {
        }

        public static void AssistVersion(NetState state, CircularBufferReader reader)
        {
            var unk = reader.ReadInt32();
            var av = reader.ReadAscii();
        }

        public static void ClientVersion(NetState state, CircularBufferReader reader)
        {
            var version = state.Version = new CV(reader.ReadAscii());

            EventSink.InvokeClientVersionReceived(state, version);
        }

        public static void ClientType(NetState state, CircularBufferReader reader)
        {
            reader.ReadUInt16();

            int type = reader.ReadUInt16();
            var version = state.Version = new CV(reader.ReadAscii());

            EventSink.InvokeClientVersionReceived(state, version);
        }

        public static void PlayCharacter(NetState state, CircularBufferReader reader)
        {
            reader.ReadInt32(); // 0xEDEDEDED

            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);

            var flags = reader.ReadInt32();

            reader.Seek(24, SeekOrigin.Current);

            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();

            var a = state.Account;

            if (a == null || charSlot < 0 || charSlot >= a.Length)
            {
                state.Dispose();
            }
            else
            {
                var m = a[charSlot];

                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal && check != m)
                    {
                        state.WriteConsole("Account in use");
                        state.SendPopupMessage(PMMessage.CharInWorld);
                        return;
                    }
                }

                if (m == null)
                {
                    state.Dispose();
                    return;
                }

                m.NetState?.Dispose();

                // TODO: Make this wait one tick so we don't have to call it unnecessarily
                NetState.ProcessDisposedQueue();

                state.SendClientVersionRequest();

                state.BlockAllPackets = true;

                state.Flags = (ClientFlags)flags;

                state.Mobile = m;
                m.NetState = state;

                new LoginTimer(state, m).Start();
            }
        }

        public static void DoLogin(this NetState state, Mobile m)
        {
            state.SendLoginConfirmation(m);

            state.SendMapChange(m.Map);

            if (!Core.SE && state.ProtocolChanges < ProtocolChanges.Version6000)
            {
                state.SendMapPatches();
            }

            state.Send(SeasonChange.Instantiate(m.GetSeason(), true));

            state.SendSupportedFeature();

            state.Sequence = 0;

            state.Send(new MobileUpdate(m, state.StygianAbyss));
            state.Send(new MobileUpdate(m, state.StygianAbyss));

            m.CheckLightLevels(true);

            state.Send(new MobileUpdate(m, state.StygianAbyss));

            state.Send(new MobileIncoming(state, m, m));
            // state.Send( new MobileAttributes( m ) );
            state.Send(new MobileStatus(m, m));
            state.SendSetWarMode(m.Warmode);

            m.SendEverything();

            state.SendSupportedFeature();
            state.Send(new MobileUpdate(m, state.StygianAbyss));
            // state.Send( new MobileAttributes( m ) );
            state.Send(new MobileStatus(m, m));
            state.SendSetWarMode(m.Warmode);
            state.Send(new MobileIncoming(state, m, m));

            state.SendLoginComplete();
            state.Send(new CurrentTime());
            state.Send(SeasonChange.Instantiate(m.GetSeason(), true));
            state.SendMapChange(m.Map);

            EventSink.InvokeLogin(m);
        }

        private static int GenerateAuthID(this NetState state)
        {
            if (m_AuthIDWindow.Count == m_AuthIDWindowSize)
            {
                var oldestID = 0;
                var oldest = DateTime.MaxValue;

                foreach (var kvp in m_AuthIDWindow)
                {
                    if (kvp.Value.Age < oldest)
                    {
                        oldestID = kvp.Key;
                        oldest = kvp.Value.Age;
                    }
                }

                m_AuthIDWindow.Remove(oldestID);
            }

            int authID;

            do
            {
                authID = Utility.Random(1, int.MaxValue - 1);

                if (Utility.RandomBool())
                {
                    authID |= 1 << 31;
                }
            } while (m_AuthIDWindow.ContainsKey(authID));

            m_AuthIDWindow[authID] = new AuthIDPersistence(state.Version);

            return authID;
        }

        public static void GameLogin(NetState state, CircularBufferReader reader)
        {
            if (state.SentFirstPacket)
            {
                state.Dispose();
                return;
            }

            state.SentFirstPacket = true;

            var authID = reader.ReadInt32();

            if (
                !m_AuthIDWindow.TryGetValue(authID, out var ap) ||
                state.m_AuthID != 0 && authID != state.m_AuthID ||
                state.m_AuthID == 0 && authID != state.m_Seed
            )
            {
                state.WriteConsole("Invalid client detected, disconnecting");
                state.Dispose();
                return;
            }

            m_AuthIDWindow.Remove(authID);
            state.Version = ap.Version;

            var username = reader.ReadAscii(30);
            var password = reader.ReadAscii(30);

            var e = new GameLoginEventArgs(state, username, password);

            EventSink.InvokeGameLogin(e);

            if (e.Accepted)
            {
                state.CityInfo = e.CityInfo;
                state.CompressionEnabled = true;
                state.PacketEncoder = NetworkCompression.Compress;

                state.SendSupportedFeature();
                state.SendCharacterList();
            }
            else
            {
                state.Dispose();
            }
        }

        public static void PlayServer(NetState state, CircularBufferReader reader)
        {
            int index = reader.ReadInt16();
            var info = state.ServerInfo;
            var a = state.Account;

            if (info == null || a == null || index < 0 || index >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                var si = info[index];

                state.m_AuthID = GenerateAuthID(state);

                state.SentFirstPacket = false;
                state.SendPlayServerAck(si, state.m_AuthID);
            }
        }

        public static void LoginServerSeed(NetState state, CircularBufferReader reader)
        {
            state.m_Seed = reader.ReadInt32();
            state.Seeded = true;

            if (state.m_Seed == 0)
            {
                state.WriteConsole("Invalid client detected, disconnecting");
                state.Dispose();
                return;
            }

            var clientMaj = reader.ReadInt32();
            var clientMin = reader.ReadInt32();
            var clientRev = reader.ReadInt32();
            var clientPat = reader.ReadInt32();

            state.Version = new ClientVersion(clientMaj, clientMin, clientRev, clientPat);
        }

        public static void AccountLogin(NetState state, CircularBufferReader reader)
        {
            if (state.SentFirstPacket)
            {
                state.Dispose();
                return;
            }

            state.SentFirstPacket = true;

            var username = reader.ReadAscii(30);
            var password = reader.ReadAscii(30);

            var accountLoginEventArgs = new AccountLoginEventArgs(state, username, password);

            EventSink.InvokeAccountLogin(accountLoginEventArgs);

            if (accountLoginEventArgs.Accepted)
            {
                var serverListEventArgs = new ServerListEventArgs(state, state.Account);

                EventSink.InvokeServerList(serverListEventArgs);

                if (serverListEventArgs.Rejected)
                {
                    state.Account = null;
                    AccountLogin_ReplyRej(state, ALRReason.BadComm);
                }
                else
                {
                    state.ServerInfo = serverListEventArgs.Servers.ToArray();
                    state.SendAccountLoginAck();
                }
            }
            else
            {
                AccountLogin_ReplyRej(state, accountLoginEventArgs.RejectReason);
            }
        }

        private static void AccountLogin_ReplyRej(this NetState state, ALRReason reason)
        {
            state.SendAccountLoginRejected(reason);
            state.Dispose();
        }

        private class LoginTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly NetState m_State;

            public LoginTimer(NetState state, Mobile m) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_State = state;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                if (m_State == null)
                {
                    Stop();
                    return;
                }

                if (m_State.Version != null)
                {
                    m_State.BlockAllPackets = false;
                    DoLogin(m_State, m_Mobile);
                    Stop();
                }
            }
        }
    }
}
