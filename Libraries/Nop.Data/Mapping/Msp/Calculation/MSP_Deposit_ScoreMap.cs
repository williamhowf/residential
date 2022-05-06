﻿using Nop.Core.Domain.Msp.Calculation;
using Nop.Core.Domain.Msp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Data.Mapping.Msp.Calculation
{
    public partial class MSP_Deposit_ScoreMap : NopEntityTypeConfiguration<MSP_Deposit_Score>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public MSP_Deposit_ScoreMap()
        {
            this.ToTable("MSP_Deposit_Score");
            this.HasKey(p => p.Id);

            this.Ignore(p => p.AmountTypeEnum);
            this.Ignore(p => p.TransactionTypeEnum);
            this.Ignore(p => p.StatusEnum); 
        }
    }
}
