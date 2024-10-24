﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace ClinicConnect.Models;

public partial class Medicine
{
    public int MedId { get; set; }

    public int? PharmMemberId { get; set; }

    public string MedName { get; set; }

    public decimal? MedPrice { get; set; }

    public string MedBrand { get; set; }

    public bool MedAvailability { get; set; }

    public virtual ICollection<BillingMedicine> BillingMedicines { get; set; } = new List<BillingMedicine>();

    public virtual Pharmacist PharmMember { get; set; }
}