using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace OPenAIPDSQandA.Services;

public class DocumentService
{
    public async Task<string> LoadDocumentAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => await Task.Run(() => LoadPdf(filePath)),
            ".txt" => await File.ReadAllTextAsync(filePath),
            ".md" => await File.ReadAllTextAsync(filePath),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Use .pdf or .txt files.")
        };
    }

    private static string LoadPdf(string filePath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(filePath);

        foreach (Page page in document.GetPages())
        {
            sb.AppendLine($"[Page {page.Number}]");
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public string GetSamplePds()
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory, "SampleData", "sample-travel-pds.txt");

        if (File.Exists(samplePath))
            return File.ReadAllText(samplePath);

        return GetEmbeddedSamplePds();
    }

    private static string GetEmbeddedSamplePds() => """
        WANDERLUST TRAVEL INSURANCE
        PRODUCT DISCLOSURE STATEMENT (PDS)
        Effective Date: 1 January 2025
        PDS Version: 3.2

        IMPORTANT: This document is the legal contract between you and Wanderlust Insurance Pty Ltd 
        (ABN 12 345 678 901, AFSL 123456). Please read this PDS carefully before purchasing.

        ================================================================
        SECTION 1 – ABOUT THIS POLICY
        ================================================================

        1.1 WHO IS COVERED
        This policy covers:
        - The primary insured person named on the Certificate of Insurance
        - Your spouse or domestic partner travelling with you
        - Dependent children under 21 years of age travelling with you

        1.2 TYPES OF COVER
        We offer three levels of cover:
        - Comprehensive: Highest level of cover, includes all benefits
        - Standard: Mid-level cover with most standard travel benefits
        - Basic: Essential cover for budget-conscious travellers

        ================================================================
        SECTION 2 – MEDICAL AND EMERGENCY EXPENSES
        ================================================================

        2.1 OVERSEAS EMERGENCY MEDICAL EXPENSES
        Comprehensive: Unlimited
        Standard: Up to AUD $5,000,000
        Basic: Up to AUD $500,000

        We will pay for reasonable and necessary medical treatment expenses incurred overseas 
        as a result of an unexpected illness or injury during your trip, including:
        - Emergency hospital accommodation and treatment
        - Emergency dental treatment (up to AUD $500 for pain relief)
        - Medical evacuation to the nearest appropriate medical facility
        - Repatriation to Australia when medically necessary

        2.2 PRE-EXISTING MEDICAL CONDITIONS
        Pre-existing medical conditions are generally not covered unless:
        a) You apply for and are granted a specific cover extension (additional premium applies)
        b) The condition is listed in our automatic cover conditions (see Section 2.3)

        2.3 AUTOMATIC COVER FOR CERTAIN CONDITIONS
        The following conditions are automatically covered at no extra cost:
        - Acne
        - Asthma (mild to moderate, well-controlled)
        - Cataracts
        - Coeliac disease
        - Colour blindness
        - Controlled diabetes (Type 1 or Type 2, no complications)
        - Ear grommets
        - Epilepsy (well-controlled, no changes to medication in last 12 months)
        - Hay fever
        - Hip/knee replacement (surgery more than 12 months prior with no complications)
        - Hypertension (well-controlled with medication, no complications)

        2.4 EMERGENCY DENTAL
        We cover emergency dental treatment to relieve acute sudden pain up to:
        Comprehensive & Standard: AUD $1,500
        Basic: AUD $500

        2.5 HOSPITAL CASH ALLOWANCE
        If you are hospitalised overseas for more than 48 hours:
        Comprehensive: AUD $100 per day (max AUD $3,000)
        Standard: AUD $75 per day (max AUD $1,500)
        Basic: Not covered

        ================================================================
        SECTION 3 – CANCELLATION AND TRIP INTERRUPTION
        ================================================================

        3.1 CANCELLATION COVER
        We will reimburse non-refundable travel and accommodation costs if you must cancel 
        your trip due to:
        - Your unexpected serious illness, injury or death
        - Serious illness, injury or death of a close relative
        - A natural disaster affecting your destination
        - Your redundancy (if employed for more than 12 months)
        - Jury service or a court subpoena
        - Travel advisories issued by DFAT

        Comprehensive: Unlimited
        Standard: Up to AUD $20,000
        Basic: Up to AUD $5,000

        3.2 TRIP INTERRUPTION
        If your trip is interrupted, we will cover:
        - Additional travel costs to return home or continue to your destination
        - Unused pre-paid travel costs

        Comprehensive: Unlimited
        Standard: Up to AUD $15,000
        Basic: Up to AUD $3,000

        3.3 WHAT IS NOT COVERED UNDER CANCELLATION
        We will not pay for cancellation due to:
        - Change of mind
        - Financial default of a travel provider (unless specified)
        - Pre-existing medical conditions (unless you have purchased an extension)
        - Travel advisories issued before you purchased this policy
        - Failure to obtain necessary travel documents (visas, passports)
        - Pregnancy occurring at the time of purchase (unless complications arise after policy purchase)

        ================================================================
        SECTION 4 – LUGGAGE AND PERSONAL EFFECTS
        ================================================================

        4.1 LUGGAGE COVER
        Comprehensive: Up to AUD $10,000
        Standard: Up to AUD $7,500
        Basic: Up to AUD $3,000

        Sub-limits apply per item:
        - Laptop/tablet: AUD $3,000 (Comprehensive), AUD $2,000 (Standard/Basic)
        - Camera equipment: AUD $3,000 (Comprehensive), AUD $1,500 (Standard/Basic)
        - Mobile phone: AUD $1,500 per item
        - Jewellery/watches: AUD $2,000 (Comprehensive), AUD $750 (Standard/Basic)
        - Sunglasses: AUD $500
        - Sports equipment: AUD $1,500

        4.2 LUGGAGE DELAY
        If your checked luggage is delayed more than 12 hours, we will reimburse essential 
        items like clothing and toiletries:
        Comprehensive: Up to AUD $500 (first 12 hrs), AUD $1,000 total
        Standard: Up to AUD $300 (first 12 hrs), AUD $600 total
        Basic: Up to AUD $200 total

        4.3 WHAT IS NOT COVERED UNDER LUGGAGE
        - Items left unattended in a vehicle unless locked in the boot
        - Fragile items (glass, ceramics) unless professionally packed
        - Cash, credit cards, travel cards (covered separately under Section 5)
        - Perishable items
        - Items over 10 years old

        ================================================================
        SECTION 5 – MONEY AND DOCUMENTS
        ================================================================

        5.1 TRAVEL DOCUMENTS
        Replacement costs for lost or stolen passports, visas, and travel documents:
        Comprehensive: Up to AUD $5,000
        Standard: Up to AUD $2,500
        Basic: Up to AUD $1,000

        5.2 CASH AND TRAVEL MONEY
        Comprehensive: Up to AUD $500
        Standard: Up to AUD $300
        Basic: Up to AUD $200

        Note: Cash must be reported to local police within 24 hours of discovery.

        5.3 CREDIT CARD FRAUD
        Comprehensive: Up to AUD $5,000
        Standard: Up to AUD $2,500
        Basic: Not covered

        ================================================================
        SECTION 6 – TRAVEL DELAYS
        ================================================================

        6.1 TRAVEL DELAY EXPENSES
        If your scheduled departure is delayed by more than 6 hours due to causes outside 
        your control (weather, strikes, mechanical breakdown), we cover additional meal and 
        accommodation expenses:

        Comprehensive: AUD $250 per 6-hour period (max AUD $2,000)
        Standard: AUD $150 per 6-hour period (max AUD $1,000)
        Basic: AUD $100 per 6-hour period (max AUD $500)

        6.2 MISSED CONNECTIONS
        If you miss a connecting flight due to a delay of the inbound flight:
        Comprehensive: Up to AUD $3,000
        Standard: Up to AUD $1,500
        Basic: Not covered

        ================================================================
        SECTION 7 – PERSONAL LIABILITY
        ================================================================

        7.1 PERSONAL LIABILITY COVER
        We cover your legal liability to pay compensation for accidental:
        - Bodily injury to a third party
        - Damage to third party property

        Comprehensive & Standard: Up to AUD $5,000,000
        Basic: Up to AUD $2,500,000

        7.2 EXCLUSIONS FROM PERSONAL LIABILITY
        - Liability arising from use of a motor vehicle, watercraft over 10m, or aircraft
        - Deliberate acts
        - Liability arising from your business or professional activities
        - Liability you assumed under a contract

        ================================================================
        SECTION 8 – ADVENTURE AND SPORTS ACTIVITIES
        ================================================================

        8.1 STANDARD COVERED ACTIVITIES
        The following activities are included as standard:
        - Swimming, snorkelling (not scuba diving)
        - Hiking/trekking up to 4,000 metres altitude
        - Skiing and snowboarding (on-piste only)
        - Cycling, mountain biking (with helmet)
        - Surfing, white-water rafting (grades 1-4)
        - Kayaking, paddleboarding
        - Bungee jumping (from licensed operators)
        - Zip-lining and canopy tours

        8.2 ACTIVITIES REQUIRING ADDITIONAL PREMIUM
        The following require the Adventure Sports Extension (additional premium):
        - Scuba diving below 30 metres
        - Rock climbing (using ropes and equipment)
        - Off-piste skiing/snowboarding
        - White-water rafting grades 5+
        - Skydiving and paragliding
        - Motorcycle riding over 200cc (when you are the rider)
        - Trekking above 4,000 metres altitude

        8.3 EXCLUDED ACTIVITIES
        Activities never covered under this policy:
        - Professional or competitive sports
        - BASE jumping
        - Free solo climbing
        - Racing of any kind
        - Use of aircraft as a pilot (passenger coverage only)

        ================================================================
        SECTION 9 – GENERAL EXCLUSIONS
        ================================================================

        This policy does not cover claims arising from:

        9.1 ALCOHOL AND DRUGS
        - Any incident where your blood alcohol level was 0.19% or above
        - Use of illegal drugs or misuse of prescription medication

        9.2 CRIMINAL ACTIVITY
        - Your own criminal acts or provoked assault

        9.3 TRAVEL ADVISORIES
        - Travel to destinations rated "Do Not Travel" (Level 4) by the Australian Government 
          DFAT at the time of travel (unless cover was purchased before the advisory was issued)
        - Travel to destinations rated "Reconsider your need to travel" (Level 3) is covered, 
          but claims directly related to the advisory reason are excluded

        9.4 WAR AND TERRORISM
        - War, civil war, or military conflict (medical costs from terrorism ARE covered)
        - Participating in civil unrest or riots

        9.5 OTHER EXCLUSIONS
        - Intentional self-harm or suicide
        - Travel for the purpose of obtaining medical treatment
        - Cosmetic or elective surgery
        - Any claim arising from pandemic/epidemic situations declared by WHO 
          (unless the Pandemic Extension is purchased)
        - Consequential losses

        ================================================================
        SECTION 10 – HOW TO MAKE A CLAIM
        ================================================================

        10.1 CLAIMS PROCESS
        1. Contact our 24/7 Emergency Assistance line: +61 2 8000 1234
        2. For non-emergency claims, lodge online at: www.wanderlust.com.au/claims
        3. Alternatively, email: claims@wanderlust.com.au
        4. Lodge your claim within 30 days of returning to Australia

        10.2 DOCUMENTS REQUIRED
        - Completed claim form (available at www.wanderlust.com.au/claims)
        - Original receipts and invoices
        - Police report (for theft/loss claims)
        - Medical certificates and reports (for medical/cancellation claims)
        - Proof of travel booking (itinerary, tickets)
        - Certificate of Insurance / policy confirmation

        10.3 CLAIM ASSESSMENT
        - Claims are assessed within 10 business days of receiving all required documentation
        - You may be asked to provide additional information
        - We will notify you of the outcome in writing

        10.4 DISPUTES
        If you disagree with our decision:
        1. Contact our Internal Dispute Resolution team: idr@wanderlust.com.au
        2. If unresolved after 30 days, contact the Australian Financial Complaints Authority (AFCA)
           - Website: www.afca.org.au | Phone: 1800 931 678

        ================================================================
        SECTION 11 – COOLING-OFF PERIOD
        ================================================================

        You have 21 days from the date of issue to cancel this policy for a full refund, 
        provided:
        - Your journey has not commenced
        - No claim has been made

        ================================================================
        SECTION 12 – DEFINITIONS
        ================================================================

        CLOSE RELATIVE: Your spouse, domestic partner, parent, parent-in-law, brother, sister, 
        child, grandparent, grandchild, brother-in-law, sister-in-law, son-in-law, daughter-in-law.

        PRE-EXISTING CONDITION: Any physical or mental health condition, disease, illness or 
        related symptom that existed prior to your policy purchase date and for which you:
        - Were aware of or should have reasonably been aware of
        - Had symptoms, received treatment, or were taking medication

        UNEXPECTED: Not anticipated or planned before the policy was purchased.

        TRIP: The journey detailed in your Certificate of Insurance, starting and ending in Australia.

        EXCESS: The amount you must pay towards each claim event. Standard excess is AUD $200.
        Comprehensive policyholders can reduce excess to $0 by selecting the Nil Excess option.

        ================================================================
        SECTION 13 – CONTACT INFORMATION
        ================================================================

        Wanderlust Insurance Pty Ltd
        ABN 12 345 678 901 | AFSL 123456

        Head Office: Level 15, 200 George Street, Sydney NSW 2000
        24/7 Emergency Assistance: +61 2 8000 1234
        General Enquiries: 1300 555 123
        Email: info@wanderlust.com.au
        Website: www.wanderlust.com.au

        This PDS is prepared and issued by Wanderlust Insurance Pty Ltd.
        Insurance is underwritten by Pacific Re Insurance Ltd (ARBN 987 654 321).

        ================================================================
        END OF PRODUCT DISCLOSURE STATEMENT
        """;
}

