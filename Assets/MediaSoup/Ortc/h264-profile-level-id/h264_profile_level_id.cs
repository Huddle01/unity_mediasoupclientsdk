using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using static h264_profile_level_id.H264PRofileLevelId;
using WebSocketSharp;
using static UnityEditor.PlayerSettings;
using Mediasoup.RtpParameter;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using UnityEditor.Presets;
using System.Security.Cryptography;

namespace h264_profile_level_id
{
    class H264PRofileLevelId
    {
        static List<ProfilePattern> profilePatterns = new List<ProfilePattern>();
        static ProfileLevelId defaultProfileLevelId = new ProfileLevelId(Profile.ConstrainedBaseline, Level.L3_1);
        static H264PRofileLevelId()
        {
            profilePatterns.Add(new ProfilePattern(0x42, new BitPattern("x1xx0000"), Profile.ConstrainedBaseline));
            profilePatterns.Add(new ProfilePattern(0x4D, new BitPattern("1xxx0000"), Profile.ConstrainedBaseline));
            profilePatterns.Add(new ProfilePattern(0x58, new BitPattern("11xx0000"), Profile.ConstrainedBaseline));
            profilePatterns.Add(new ProfilePattern(0x42, new BitPattern("x0xx0000"), Profile.Baseline));
            profilePatterns.Add(new ProfilePattern(0x58, new BitPattern("10xx0000"), Profile.Baseline));
            profilePatterns.Add(new ProfilePattern(0x4D, new BitPattern("0x0x0000"), Profile.Main));
            profilePatterns.Add(new ProfilePattern(0x64, new BitPattern("00000000"), Profile.High));
            profilePatterns.Add(new ProfilePattern(0x64, new BitPattern("00001100"), Profile.ConstrainedHigh));
            profilePatterns.Add(new ProfilePattern(0xF4, new BitPattern("00000000"), Profile.PredictiveHigh444));
        }

        public enum Profile
        {
            ConstrainedBaseline = 1,
            Baseline = 2,
            Main = 3,
            ConstrainedHigh = 4,
            High = 5,
            PredictiveHigh444 = 6
        }

        public enum Level
        {
            L1_b = 0,
            L1 = 10,
            L1_1 = 11,
            L1_2 = 12,
            L1_3 = 13,
            L2 = 20,
            L2_1 = 21,
            L2_2 = 22,
            L3 = 30,
            L3_1 = 31,
            L3_2 = 32,
            L4 = 40,
            L4_1 = 41,
            L4_2 = 42,
            L5 = 50,
            L5_1 = 51,
            L5_2 = 52
        }

        public class ProfileLevelId
        {
            public readonly Profile profile;
            public readonly Level level;

            public ProfileLevelId(Profile profile, Level level)
            {
                this.profile = profile;
                this.level = level;
            }

            private ProfileLevelId()
            {
            }
        }

        class BitPattern
        {
            public readonly int mask;
            public readonly int masked_value;

            public BitPattern(string str)
            {
                this.mask = ~byteMaskString('x', str);
                this.masked_value = byteMaskString('1', str);
            }

            public static int byteMaskString(char c, string str)
            {
                return
                ((str[0] == c ? 1 : 0) << 7) |
                ((str[1] == c ? 1 : 0) << 6) |
                ((str[2] == c ? 1 : 0) << 5) |
                ((str[3] == c ? 1 : 0) << 4) |
                ((str[4] == c ? 1 : 0) << 3) |
                ((str[5] == c ? 1 : 0) << 2) |
                ((str[6] == c ? 1 : 0) << 1) |
                ((str[7] == c ? 1 : 0) << 0);
            }

            public bool isMatch(int value)
            {
                return this.masked_value == (value & this.mask);
            }
        }

        class ProfilePattern
        {

            public readonly int profileIdc;
            public readonly BitPattern profileIop;
            public readonly Profile profile;

            public ProfilePattern(int profileIdc, BitPattern profileIop, Profile profile)
            {
                this.profileIop = profileIop;
                this.profileIdc = profileIdc;
                this.profile = profile;
            }
        }

        /// <summary>
        /// Parse profile level id that is represented as a string of 3 hex bytes.
        /// Nothing will be returned if the string is not a recognized H264 profile
        /// level id.
        /// </summary>
        /// <param name="profileLevelId">String representation of the profile level id.</param>
        /// <returns>ProfileLevelId converted from the input</returns>
        /// <exception cref="System.ArgumentException">Level 1_b not allowed for this profile.</exception>
        /// <exception cref="System.Exception"></exception>
        public ProfileLevelId parseProfileLevelId(string profileLevelId)
        {
            const int ConstraintSet3Flag = 0x10;

            if (profileLevelId == null || profileLevelId.Length != 6)
            {
                throw new System.ArgumentException("Invalid profile level id");
            }

            int profileLevelIdNumeric = 0;
            try
            {
                profileLevelIdNumeric = int.Parse(profileLevelId, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to convert profile level id into integer");
            }
            finally
            {
                if (profileLevelIdNumeric == 0)
                {
                    throw new Exception("Invalid profile level id");
                }
            }

            Level level_idc = (Level)Enum.Parse(typeof(Level), (profileLevelIdNumeric & 0xFF).ToString());
            byte profile_iop = (byte)((profileLevelIdNumeric >> 8) & 0xFF);
            byte profile_idc = (byte)((profileLevelIdNumeric >> 16) & 0xFF);


            Level level;

            switch (level_idc)
            {

                case Level.L1_1:
                    {
                        level = (profile_iop & ConstraintSet3Flag) != 0 ? Level.L1_b : Level.L1_1;
                        break;
                    }
                case Level.L1:
                case Level.L1_2:
                case Level.L1_3:
                case Level.L2:
                case Level.L2_1:
                case Level.L2_2:
                case Level.L3:
                case Level.L3_1:
                case Level.L3_2:
                case Level.L4:
                case Level.L4_1:
                case Level.L4_2:
                case Level.L5:
                case Level.L5_1:
                case Level.L5_2:
                    {
                        level = level_idc;

                        break;
                    }
                default:
                    {
                        return null;
                    }
            }

            foreach (ProfilePattern pattern in profilePatterns)
            {
                if (pattern.profileIdc == profile_idc && pattern.profileIop.isMatch(profile_iop))
                {
                    return new ProfileLevelId(pattern.profile, level);
                }
            }

            Console.WriteLine("parseProfileLevelId() | unrecognized profile_idc/profile_iop combination [str:" + profileLevelId
                + ", profile_idc:" + profile_idc + ", profile_iop:" + profile_iop);

            return null;
        }

        /// <summary>
        /// Returns canonical string representation as three hex bytes of the profile
        ///   level id, or returns nothing for invalid profile level ids.
        /// </summary>
        /// <param name="profileLevelId">A ProfileLevelId type</param>
        /// <returns>A string if successful, otherwise null.</returns>
        /// <exception cref="System.ArgumentNullException">enumType or value is null.</exception>
        /// <exception cref="System.ArgumentException">Level 1_b not allowed for this profile.</exception>
        public string profileLevelIdToString(ProfileLevelId profileLevelId)
        {
            if (profileLevelId == null)
            {
                throw new System.ArgumentNullException("Invalid profile level id");
            }

            if (profileLevelId.level == Level.L1_b)
            {
                switch (profileLevelId.profile)
                {
                    case Profile.ConstrainedBaseline:
                        {
                            return "42f00b";
                        }

                    case Profile.Baseline:
                        {
                            return "42100b";
                        }

                    case Profile.Main:
                        {
                            return "4d100b";
                        }

                    // Level 1_b is not allowed for other profiles.
                    default:
                        {
                            Console.WriteLine("profileLevelIdToString() | Level 1_b not is allowed for profile " + profileLevelId.profile);
                            throw new System.ArgumentException("profileLevelIdToString() | Level 1_b not is allowed for profile " + profileLevelId.profile);
                        }
                }
            }

            string profile_idc_iop_string;

            switch (profileLevelId.profile)
            {
                case Profile.ConstrainedBaseline:
                    {
                        profile_idc_iop_string = "42e0";

                        break;
                    }

                case Profile.Baseline:
                    {
                        profile_idc_iop_string = "4200";

                        break;
                    }

                case Profile.Main:
                    {
                        profile_idc_iop_string = "4d00";

                        break;
                    }

                case Profile.ConstrainedHigh:
                    {
                        profile_idc_iop_string = "640c";

                        break;
                    }

                case Profile.High:
                    {
                        profile_idc_iop_string = "6400";

                        break;
                    }

                case Profile.PredictiveHigh444:
                    {
                        profile_idc_iop_string = "f400";

                        break;
                    }
                default:
                    {
                        Console.WriteLine("profileLevelIdToString() | unrecognized profile " + profileLevelId.profile);

                        throw new System.ArgumentException("profileLevelIdToString() | unrecognized profile " + profileLevelId.profile);
                    }
            }

            string levelStr = ((int)profileLevelId.level).ToString("X2");

            if (levelStr.Length == 1)
            {
                levelStr = "0" + levelStr;
            }


            return profile_idc_iop_string + levelStr;
        }

        /// <summary>
        /// Returns a human friendly name for the given profile.
        /// </summary>
        /// <param name="profile">Profile to interpret the name from</param>
        /// <returns>
        /// <para>string: If appropriate profile was provided</para>
        /// <para>null: Unrecognised profile provided</para>
        /// </returns>   
        public string profileToString(Profile profile)
        {
            switch (profile)
            {

                case Profile.ConstrainedBaseline:
                    {
                        return "ConstrainedBaseline";
                    }

                case Profile.Baseline:
                    {
                        return "Baseline";
                    }

                case Profile.Main:
                    {
                        return "Main";
                    }

                case Profile.ConstrainedHigh:
                    {
                        return "ConstrainedHigh";
                    }

                case Profile.High:
                    {
                        return "High";
                    }

                case Profile.PredictiveHigh444:
                    {
                        return "PredictiveHigh444";
                    }

                default:
                    {
                        Console.WriteLine("profileToString() | unrecognized profile" + profile.ToString());

                        return null;
                    }
            }
        }


        /// <summary>
        /// Returns a human friendly name for the given level.
        /// </summary>
        /// <param name="level">level to extract name from</param>
        /// <returns>
        /// <para>string: If appropriate level was provided</para>
        /// <para>null: Unrecognised unrecognised provided</para>
        /// </returns>  
        public string levelToString(Level level)
        {
            switch (level)
            {
                case Level.L1_b:
                    {
                        return "1b";

                    }

                case Level.L1:
                    {
                        return "1";
                    }

                case Level.L1_1:
                    {
                        return "1.1";
                    }

                case Level.L1_2:
                    {
                        return "1.2";

                    }

                case Level.L1_3:
                    {
                        return "1.3";

                    }

                case Level.L2:
                    {
                        return "2";

                    }

                case Level.L2_1:
                    {
                        return "2.1";

                    }

                case Level.L2_2:
                    {
                        return "2.2";

                    }

                case Level.L3:
                    {
                        return "3";

                    }

                case Level.L3_1:
                    {
                        return "3.1";
                    }

                case Level.L3_2:
                    {
                        return "3.2";
                    }

                case Level.L4:
                    {
                        return "4";
                    }

                case Level.L4_1:
                    {
                        return "4.1";
                    }

                case Level.L4_2:
                    {
                        return "4.2";
                    }

                case Level.L5:
                    {
                        return "5";
                    }

                case Level.L5_1:
                    {
                        return "5.1";
                    }

                case Level.L5_2:
                    {
                        return "5.2";
                    }

                default:
                    {
                        Console.WriteLine("levelToString() | unrecognized level" + level);

                        return null;
                    }
            }
        }

        /// <summary>
        ///   Parse profile level id that is represented as a string of 3 hex bytes
        ///   contained in an SDP key-value map.A default profile level id will be
        ///   returned if the profile-level-id key is missing.Nothing will be returned
        ///   if the key is present but the string is invalid.
        /// </summary>
        /// <param name="parameters">parameters</param>
        /// <returns>ProfileLevelId from the profile-level-id key in the parameters</returns>
        /// <exception cref="System.ArgumentNullException">On failure to parse the profile level id</exception>
        public ProfileLevelId parseSdpProfileLevelId(Dictionary<string, string> parameters)
        {

            string profileLevelId = parameters["profile-level-id"];

            ProfileLevelId parsedProfileLevelId = parseProfileLevelId(profileLevelId);

            if (parsedProfileLevelId != null)
            {
                return parsedProfileLevelId;
            }

            return defaultProfileLevelId;
        }

        /// <summary>
        /// Returns true if the parameters have the same H264 profile, i.e. the same
        /// H264 profile (Baseline, High, etc)
        /// </summary>
        /// <param name="parametersA">Dictionary of parameters of RTPCodecCapability of RTPCodecParameter.</param>
        /// <param name="parametersB">Dictionary of parameters of RTPCodecCapability of RTPCodecParameter.</param>
        /// <returns>A string representing the codec parameters for the answer.</returns>
        /// <exception cref="System.ArgumentNullException">On failure to parse the profile level id</exception>
        public bool isSameProfile(Dictionary<string, string> parametersA, Dictionary<string, string> parametersB)
        {

            ProfileLevelId profileIdA = parseSdpProfileLevelId(parametersA);
            ProfileLevelId profileIdB = parseSdpProfileLevelId(parametersB);

            if (profileIdA == null || profileIdB == null)
            {
                return false;
            }

            return profileIdA.profile == profileIdB.profile;
        }

        /// <summary>
        /// Generate codec parameters that will be used as answer in an SDP negotiation
        /// based on local supported parameters and remote offered parameters. Both
        /// local_supported_params and remote_offered_params represent sendrecv media
        /// descriptions, i.e., they are a mix of both encode and decode capabilities. In
        /// theory, when the profile in local_supported_params represents a strict
        /// superset of the profile in remote_offered_params, we could limit the profile
        /// in the answer to the profile in remote_offered_params.
        ///
        /// However, to simplify the code, each supported H264 profile should be listed
        /// explicitly in the list of local supported codecs, even if they are redundant.
        /// Then each local codec in the list should be tested one at a time against the
        /// remote codec, and only when the profiles are equal should this function be
        /// called. Therefore, this function does not need to handle profile intersection,
        /// and the profile of local_supported_params and remote_offered_params must be
        /// equal before calling this function. The parameters that are used when
        /// negotiating are the level part of profile-level-id and
        /// level-asymmetry-allowed.
        /// </summary>
        /// <param name="local_supported_params">Dictionary representing local supported parameters.</param>
        /// <param name="remote_offered_params">Dictionary representing remote offered parameters.</param>
        /// <returns>A string representing the codec parameters for the answer.</returns>
        /// <exception cref="System.ArgumentNullException">On failure to parse the profile level id</exception>

        public string generateProfileLevelIdStringForAnswer(Dictionary<string, string> local_supported_params,
            Dictionary<string, string> remote_offered_params)
        {

            if (local_supported_params == null || remote_offered_params == null)
            {
                throw new System.ArgumentNullException("Invalid local_supported_params or remote_offered_params");
            }

            if (!local_supported_params.ContainsKey("profile-level-id") && !remote_offered_params.ContainsKey("profile-level-id"))
            {
                throw new System.ArgumentException("Invalid local_supported_params or remote_offered_params");
            }

            ProfileLevelId localProfileLevelId, remoteProfileLevelId;

            try
            {
                localProfileLevelId = parseSdpProfileLevelId(local_supported_params);
            }
            catch (Exception e)
            {
                Console.WriteLine("generateProfileLevelIdStringForAnswer() | failed to parse local profile-level-id");
                return null;
            }

            try
            {
                remoteProfileLevelId = parseSdpProfileLevelId(remote_offered_params);
            }
            catch (Exception e)
            {
                Console.WriteLine("generateProfileLevelIdStringForAnswer() | failed to parse remote profile-level-id");
                return null;
            }



            if (localProfileLevelId == null)
            {
                throw new Exception("Invalid local profile-level-id");
            }

            if (remoteProfileLevelId == null)
            {
                throw new Exception("Invalid remote profile-level-id");
            }

            bool levelAsymmetryAllowed = (isLevelAsymmetryAllowed(local_supported_params) && isLevelAsymmetryAllowed(remote_offered_params));

            Level localLevel = localProfileLevelId.level;
            Level remoteLevel = remoteProfileLevelId.level;
            Level min_level = minLevel(localLevel, remoteLevel);

            Level answerLevel = levelAsymmetryAllowed ? localLevel : min_level;

            return profileLevelIdToString(new ProfileLevelId(localProfileLevelId.profile, answerLevel));
        }

        private bool isLevelAsymmetryAllowed(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("level-asymmetry-allowed"))
            {
                return parameters["level-asymmetry-allowed"] == "1";
            }

            return false;
        }

        private bool isLessLevel(Level levelA, Level levelB)
        {
            if (levelA == Level.L1_b)
            {
                return levelB != Level.L1 && levelB != Level.L1_b;
            }

            if (levelB == Level.L1_b)
            {
                return levelA != Level.L1;
            }

            return levelA < levelB;
        }

        private Level minLevel(Level levelA, Level levelB)
        {
            return isLessLevel(levelA, levelB) ? levelA : levelB;
        }
    }
}