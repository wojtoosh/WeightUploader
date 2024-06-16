#region Copyright
/////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2024 Garmin International, Inc.
// Licensed under the Flexible and Interoperable Data Transfer (FIT) Protocol License; you
// may not use this file except in compliance with the Flexible and Interoperable Data
// Transfer (FIT) Protocol License.
/////////////////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  This file is auto-generated!  Do NOT edit this file.
// Profile Version = 21.141.0Release
// Tag = production/release/21.141.0-0-g2aa27e1
/////////////////////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;

namespace Dynastream.Fit
{
    /// <summary>
    /// Implements the ChronoShotData profile message.
    /// </summary>
    public class ChronoShotDataMesg : Mesg
    {
        #region Fields
        #endregion

        /// <summary>
        /// Field Numbers for <see cref="ChronoShotDataMesg"/>
        /// </summary>
        public sealed class FieldDefNum
        {
            public const byte Timestamp = 253;
            public const byte ShotSpeed = 0;
            public const byte ShotNum = 1;
            public const byte Invalid = Fit.FieldNumInvalid;
        }

        #region Constructors
        public ChronoShotDataMesg() : base(Profile.GetMesg(MesgNum.ChronoShotData))
        {
        }

        public ChronoShotDataMesg(Mesg mesg) : base(mesg)
        {
        }
        #endregion // Constructors

        #region Methods
        ///<summary>
        /// Retrieves the Timestamp field</summary>
        /// <returns>Returns DateTime representing the Timestamp field</returns>
        public DateTime GetTimestamp()
        {
            Object val = GetFieldValue(253, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return TimestampToDateTime(Convert.ToUInt32(val));
            
        }

        /// <summary>
        /// Set Timestamp field</summary>
        /// <param name="timestamp_">Nullable field value to be set</param>
        public void SetTimestamp(DateTime timestamp_)
        {
            SetFieldValue(253, 0, timestamp_.GetTimeStamp(), Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ShotSpeed field
        /// Units: m/s</summary>
        /// <returns>Returns nullable float representing the ShotSpeed field</returns>
        public float? GetShotSpeed()
        {
            Object val = GetFieldValue(0, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToSingle(val));
            
        }

        /// <summary>
        /// Set ShotSpeed field
        /// Units: m/s</summary>
        /// <param name="shotSpeed_">Nullable field value to be set</param>
        public void SetShotSpeed(float? shotSpeed_)
        {
            SetFieldValue(0, 0, shotSpeed_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ShotNum field</summary>
        /// <returns>Returns nullable ushort representing the ShotNum field</returns>
        public ushort? GetShotNum()
        {
            Object val = GetFieldValue(1, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToUInt16(val));
            
        }

        /// <summary>
        /// Set ShotNum field</summary>
        /// <param name="shotNum_">Nullable field value to be set</param>
        public void SetShotNum(ushort? shotNum_)
        {
            SetFieldValue(1, 0, shotNum_, Fit.SubfieldIndexMainField);
        }
        
        #endregion // Methods
    } // Class
} // namespace
