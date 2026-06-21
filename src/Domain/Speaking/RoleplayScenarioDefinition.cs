using System.Security.Cryptography;
using System.Text;
using Domain.Assessment;
using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// The curated definition of a roleplay scenario: the persona the tutor plays, the setting, and the
/// objective the learner must accomplish. All text here is English on purpose - it feeds the LLM
/// persona prompt, and the LLM always thinks in English (docs/development-guide.md rule 11). The learner-facing Uzbek
/// labels are looked up in the frontend content store by <see cref="Code"/>. This is pure curated
/// data with no dependencies, so it lives in the domain and is server-authoritative (the client only
/// ever sends the scenario code, never the persona text).
/// </summary>
public sealed class RoleplayScenarioDefinition
{
    public RoleplayScenarioDefinition(
        string code,
        string englishTitle,
        string personaRole,
        string setting,
        string learnerObjective,
        CefrLevel level)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Roleplay scenario code must not be empty.");
        if (string.IsNullOrWhiteSpace(englishTitle))
            throw new DomainException("Roleplay scenario English title must not be empty.");
        if (string.IsNullOrWhiteSpace(personaRole))
            throw new DomainException("Roleplay persona role must not be empty.");
        if (string.IsNullOrWhiteSpace(setting))
            throw new DomainException("Roleplay setting must not be empty.");
        if (string.IsNullOrWhiteSpace(learnerObjective))
            throw new DomainException("Roleplay learner objective must not be empty.");

        Code = code;
        EnglishTitle = englishTitle;
        PersonaRole = personaRole;
        Setting = setting;
        LearnerObjective = learnerObjective;
        Level = level;
        ImageId = CreateImageId(code);
    }

    /// <summary>Stable snake_case code used on the API and to key the Uzbek content-store labels.</summary>
    public string Code { get; }

    /// <summary>English title (reference/debug); the UI shows the Uzbek label from the content store.</summary>
    public string EnglishTitle { get; }

    /// <summary>The role the tutor plays, e.g. "a friendly but professional job interviewer".</summary>
    public string PersonaRole { get; }

    /// <summary>Where the scene takes place, phrased for the LLM.</summary>
    public string Setting { get; }

    /// <summary>What the learner is trying to achieve in the scene, phrased for the LLM.</summary>
    public string LearnerObjective { get; }

    /// <summary>The single CEFR level this scenario is curated for (20 scenarios per level).</summary>
    public CefrLevel Level { get; }

    /// <summary>
    /// A stable, deterministic id (derived from <see cref="Code"/>) that keys this scenario's
    /// illustration in the shared topic-image store, so scenarios reuse the whole licensed-image
    /// pipeline (download once, store in DB, serve from /api/images/topics/{id} - rule 12) without
    /// needing their own table. Deterministic so a re-seeded catalog keeps its images.
    /// </summary>
    public Guid ImageId { get; }

    // Mirrors Infrastructure.Common.DeterministicGuid (MD5 of the key) - duplicated here because the
    // domain references nothing. Not security-relevant; only a reproducible id.
    private static Guid CreateImageId(string code) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"roleplay-scenario:{code}")));
}

/// <summary>
/// The fixed catalog of roleplay scenarios: exactly <see cref="ScenariosPerLevel"/> real-life
/// situations for each CEFR level (A1→C2), <see cref="TotalScenarios"/> in total. Curated content
/// (docs/development-guide.md: not AI-invented), kept as a static domain table like <see cref="FreeTalkTopicCatalog"/>
/// so the tutor prompt builder, the evaluator and the scenarios query resolve exactly the same
/// definitions. Difficulty steps up by level: simple transactional scenes at A1/A2 (a café, a taxi),
/// negotiation and problem-solving at B1/B2 (a complaint, a salary talk), and high-stakes
/// professional/rhetorical scenes at C1/C2 (a board meeting, a live TV debate).
/// </summary>
public static class RoleplayScenarioCatalog
{
    /// <summary>How many scenarios every CEFR level offers.</summary>
    public const int ScenariosPerLevel = 20;

    /// <summary>Total scenarios across all six CEFR levels.</summary>
    public const int TotalScenarios = ScenariosPerLevel * 6;

    private static readonly IReadOnlyList<RoleplayScenarioDefinition> Scenarios = BuildCatalog();

    private static readonly IReadOnlyDictionary<string, RoleplayScenarioDefinition> ByCode =
        Scenarios.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>All scenarios, ordered by level (A1→C2) then by their curated order within the level.</summary>
    public static IReadOnlyList<RoleplayScenarioDefinition> All => Scenarios;

    /// <summary>The 20 scenarios curated for a single CEFR level, in their curated order.</summary>
    public static IReadOnlyList<RoleplayScenarioDefinition> ForLevel(CefrLevel level) =>
        Scenarios.Where(s => s.Level == level).ToList();

    /// <summary>True when <paramref name="code"/> is a known scenario code (used by validators).</summary>
    public static bool Contains(string? code) =>
        !string.IsNullOrWhiteSpace(code) && ByCode.ContainsKey(code);

    /// <summary>Resolves the definition for a scenario code; throws when the code is unknown.</summary>
    public static RoleplayScenarioDefinition Get(string code) =>
        !string.IsNullOrWhiteSpace(code) && ByCode.TryGetValue(code, out var definition)
            ? definition
            : throw new DomainException($"Unknown roleplay scenario: {code}.");

    private static IReadOnlyList<RoleplayScenarioDefinition> BuildCatalog()
    {
        var scenarios = new List<RoleplayScenarioDefinition>(TotalScenarios);

        void AddLevel(
            CefrLevel level,
            params (string Code, string Title, string Persona, string Setting, string Objective)[] entries)
        {
            foreach (var (code, title, persona, setting, objective) in entries)
                scenarios.Add(new RoleplayScenarioDefinition(code, title, persona, setting, objective, level));
        }

        // A1 - simple, transactional survival scenes: short exchanges, concrete vocabulary.
        AddLevel(
            CefrLevel.A1,
            ("airport", "At the Airport",
                "a polite airport check-in agent",
                "the check-in desk at an international airport",
                "check in for your flight: give your name, hand over your passport, choose a seat, "
                    + "and ask about your luggage"),
            ("restaurant", "At the Restaurant",
                "a warm, attentive waiter",
                "a sit-down restaurant during dinner service",
                "order food and drinks, ask a question about the menu, and ask for the bill"),
            ("shop", "At the Shop",
                "a helpful shop assistant in a clothing store",
                "a clothing shop",
                "find an item you like, ask about the size, colour and price, and pay for it"),
            ("cafe", "At the Café",
                "a friendly barista",
                "the counter of a small coffee shop",
                "order a drink and a snack, ask the price, and pay"),
            ("taxi", "Taking a Taxi",
                "a chatty but polite taxi driver",
                "a taxi ride across the city",
                "tell the driver where you want to go, ask how long it takes and how much it costs, "
                    + "and pay at the end"),
            ("hotel_check_in", "Hotel Check-in",
                "a welcoming hotel receptionist",
                "the front desk of a small hotel",
                "check in: give your name, confirm your booking, and ask about breakfast and Wi-Fi"),
            ("bus_ticket", "Buying a Bus Ticket",
                "a patient ticket clerk at a bus station",
                "the ticket window of a bus station",
                "buy a ticket: say where you are going, and ask about the departure time and the price"),
            ("bakery", "At the Bakery",
                "a cheerful baker",
                "a small neighbourhood bakery in the morning",
                "buy some bread and something sweet, ask what is fresh today, and pay"),
            ("fruit_market", "At the Fruit Market",
                "a friendly fruit seller",
                "a busy street-market fruit stall",
                "buy some fruit: ask the price per kilo, choose how much you want, and pay"),
            ("asking_directions", "Asking for Directions",
                "a helpful local person",
                "a street corner in a town the learner does not know",
                "ask the way to the train station and make sure you understand the directions"),
            ("new_neighbor", "Meeting a New Neighbour",
                "a warm, curious new neighbour",
                "the hallway of an apartment building",
                "introduce yourself, say where you are from, and make simple small talk"),
            ("pharmacy", "At the Pharmacy",
                "a kind pharmacist",
                "the counter of a small pharmacy",
                "ask for something for a headache, ask how and when to take it, and pay"),
            ("post_office", "At the Post Office",
                "a helpful post office clerk",
                "the counter of a post office",
                "send a letter abroad: say where it is going, ask the price, and buy a stamp"),
            ("supermarket", "At the Supermarket",
                "a helpful supermarket employee",
                "the aisles of a large supermarket",
                "ask where to find milk, bread and eggs, and ask about the price of one item"),
            ("pizza_order", "Ordering Pizza by Phone",
                "a friendly pizza shop worker taking phone orders",
                "a phone call to a pizza shop",
                "order a pizza: choose the size and toppings, give your address, and ask when it will arrive"),
            ("lost_bag", "A Lost Bag",
                "a calm information-desk assistant",
                "the information desk of a shopping centre",
                "explain that you lost your bag, describe what it looks like, and answer simple questions"),
            ("zoo_tickets", "Tickets for the Zoo",
                "a friendly ticket seller",
                "the ticket booth at a city zoo",
                "buy tickets for your family, ask the price for children, and ask when the zoo closes"),
            ("library_card", "Getting a Library Card",
                "a quiet, helpful librarian",
                "the front desk of a public library",
                "ask for a library card: give your name and address, and ask how many books you can borrow"),
            ("ice_cream_stand", "At the Ice Cream Stand",
                "a cheerful ice cream seller",
                "an ice cream stand in a sunny park",
                "choose flavours for you and a friend, ask about sizes and prices, and pay"),
            ("new_classmate", "Meeting a New Classmate",
                "a friendly new classmate",
                "the first day of an English class",
                "introduce yourself, ask the classmate's name and hobbies, and agree to study together"));

        // A2 - everyday problems and arrangements: past events, simple explanations and requests.
        AddLevel(
            CefrLevel.A2,
            ("job_interview", "Job Interview",
                "a friendly but professional job interviewer named Mr. Bell",
                "a job interview at a company the learner is applying to",
                "introduce yourself, answer questions about your experience and strengths, "
                    + "and explain why you want the job"),
            ("doctor", "At the Doctor",
                "a calm, caring doctor named Dr. Smith",
                "a doctor's consultation room",
                "describe your symptoms, answer the doctor's questions, and make sure you understand "
                    + "the advice"),
            ("hairdresser", "At the Hairdresser",
                "a talkative, friendly hairdresser",
                "a chair at a busy hair salon",
                "explain what haircut you want, answer the hairdresser's questions, and make small talk"),
            ("bank_account", "Opening a Bank Account",
                "a professional, patient bank clerk",
                "a desk at a local bank branch",
                "open a simple bank account: give your details, ask what documents you need, "
                    + "and ask about the bank card"),
            ("lost_luggage", "Lost Luggage",
                "a patient airline lost-luggage agent",
                "the lost-luggage desk at an airport",
                "report your missing suitcase: describe it, say which flight you were on, "
                    + "and give an address for delivery"),
            ("train_station", "At the Train Station",
                "a busy but helpful railway ticket officer",
                "the ticket office of a train station",
                "buy a return ticket, ask about the platform and departure time, and ask if there is "
                    + "a discount"),
            ("bike_rental", "Renting a Bike",
                "a laid-back bike rental assistant",
                "a bike rental shop near a park",
                "rent a bike for the afternoon: ask the hourly price, the deposit, and the closing time"),
            ("hotel_problem", "A Problem at the Hotel",
                "an apologetic hotel receptionist",
                "the front desk of a hotel in the evening",
                "complain politely that your room is cold and noisy, and ask to change to another room"),
            ("return_item", "Returning an Item",
                "a polite customer-service assistant",
                "the returns desk of a department store",
                "return a shirt that does not fit: explain the problem, show the receipt, and ask for "
                    + "your money back or an exchange"),
            ("clinic_call", "Booking a Doctor's Appointment",
                "a friendly medical receptionist on the phone",
                "a phone call to a local clinic",
                "book an appointment: explain briefly what is wrong, choose a day and time, "
                    + "and ask what to bring"),
            ("gym_signup", "Joining a Gym",
                "an energetic gym receptionist",
                "the reception of a fitness club",
                "join the gym: ask about prices and opening hours, and ask if you can try one "
                    + "class for free"),
            ("food_delivery", "Ordering Food Delivery",
                "a courteous restaurant worker taking phone orders",
                "a phone order for home delivery",
                "order dinner for two people, give your address, and ask about the delivery time "
                    + "and payment"),
            ("phone_shop", "Buying a Phone",
                "a tech-savvy phone shop assistant",
                "a mobile phone shop",
                "buy a new phone: describe what you need, compare two models, and ask about "
                    + "the guarantee"),
            ("dentist", "At the Dentist",
                "a reassuring dentist",
                "a dental clinic examination room",
                "explain your tooth pain: say when it started, answer the dentist's questions, "
                    + "and make sure you understand the treatment"),
            ("tourist_info", "At the Tourist Information Centre",
                "a knowledgeable tourist information officer",
                "the tourist information centre of a city you are visiting",
                "ask what to see in one day, get a free map, and ask about a city bus tour"),
            ("birthday_invitation", "Inviting a Friend",
                "an old friend who is happy to hear from you",
                "a phone call with a friend",
                "invite your friend to your birthday party: say when and where it is, "
                    + "and answer their questions"),
            ("cinema_tickets", "At the Cinema",
                "a helpful cinema box-office worker",
                "the box office of a cinema",
                "buy tickets for a film: ask about show times, choose seats, and ask the price "
                    + "of snacks"),
            ("flower_shop", "At the Flower Shop",
                "a warm, artistic florist",
                "a small flower shop",
                "buy flowers for a friend's birthday: describe what they like, choose colours, "
                    + "and agree a price"),
            ("talking_to_teacher", "Talking to Your Teacher",
                "a supportive English teacher",
                "a classroom after the lesson has finished",
                "ask your teacher about your homework: what you should improve and how to "
                    + "practise at home"),
            ("lost_wallet", "Reporting a Lost Wallet",
                "a calm, friendly police officer",
                "the front desk of a small police station",
                "report your lost wallet: say what was inside, where you think you lost it, "
                    + "and leave your contact details"));

        // B1 - connected explanations and light negotiation: booking changes, complaints, plans.
        AddLevel(
            CefrLevel.B1,
            ("apartment_viewing", "Viewing an Apartment",
                "a businesslike landlord showing a flat",
                "an apartment viewing at a rental flat",
                "ask about the rent, bills and house rules, describe what you need, and arrange "
                    + "the next step"),
            ("internship_interview", "Internship Interview",
                "a friendly HR coordinator",
                "an interview for a summer internship",
                "talk about your studies, skills and availability, and ask good questions about "
                    + "the role"),
            ("bank_card_issue", "A Problem with Your Card",
                "a careful bank support agent",
                "a phone call to your bank's support line",
                "explain that your card was blocked and you were charged twice, answer the "
                    + "identity questions, and agree how it will be fixed"),
            ("missed_flight", "Missing a Connection",
                "an efficient airline service agent",
                "the airline service desk after you missed a connecting flight",
                "explain what happened, ask to be rebooked on the next flight, and ask about "
                    + "a hotel or meal voucher"),
            ("restaurant_complaint", "Complaining at a Restaurant",
                "a professional restaurant manager",
                "your table at a restaurant",
                "complain politely that your dish arrived cold after a long wait, and agree "
                    + "a fair solution"),
            ("travel_agency", "At the Travel Agency",
                "an enthusiastic travel agent",
                "a travel agency office",
                "plan a one-week holiday: give your budget, dates and preferences, and compare "
                    + "two package offers"),
            ("new_coworker", "First Day at Work",
                "a welcoming coworker",
                "your first day at a new office",
                "introduce yourself, ask about the team and the daily routine, and find out the "
                    + "unwritten rules"),
            ("market_bargaining", "Bargaining at the Bazaar",
                "a shrewd but good-humoured market seller",
                "a souvenir stall at a bazaar",
                "bargain for a handmade carpet: ask the price, make a lower offer, respond to "
                    + "counter-offers, and agree a deal"),
            ("car_rental", "Renting a Car",
                "a detail-oriented car rental agent",
                "the car rental counter at an airport",
                "rent a car: ask about insurance, the fuel policy and adding a second driver, "
                    + "and check the total price"),
            ("tech_support", "Calling Tech Support",
                "a methodical IT support agent",
                "a phone call about your broken laptop",
                "describe the problem clearly, follow the agent's troubleshooting steps, and "
                    + "arrange a repair"),
            ("hotel_booking_change", "Changing a Booking",
                "a helpful hotel reservations agent",
                "a phone call to a hotel reservations desk",
                "move your booking to different dates, ask about any fees, and confirm the "
                    + "new details"),
            ("gym_cancellation", "Cancelling a Membership",
                "a persistent gym membership manager",
                "the manager's desk at your gym",
                "cancel your membership politely, resist the sales pitch to stay, and confirm "
                    + "the cancellation terms"),
            ("university_admissions", "At the Admissions Office",
                "a precise university admissions officer",
                "the admissions office of a university",
                "ask about applying: which documents you need, the deadlines, and the language "
                    + "requirements"),
            ("insurance_claim", "Making an Insurance Claim",
                "a thorough insurance agent",
                "a phone call to your insurance company",
                "report water damage in your kitchen, describe what happened and what was damaged, "
                    + "and ask about the next steps"),
            ("cake_order", "Ordering a Custom Cake",
                "a creative pastry chef",
                "the counter of a cake shop",
                "order a custom birthday cake: agree the size, design and flavours, and arrange "
                    + "the pickup day"),
            ("car_mechanic", "At the Mechanic",
                "a no-nonsense car mechanic",
                "a car repair garage",
                "describe the strange noise your car makes, ask what might be wrong, and agree "
                    + "the cost and time of the repair"),
            ("parent_teacher_meeting", "Parent-Teacher Meeting",
                "a considerate school teacher",
                "a parent-teacher meeting at school",
                "discuss your child's progress: ask about strengths and problems, and agree a "
                    + "plan to help at home"),
            ("career_advisor", "Seeing a Careers Advisor",
                "an encouraging careers advisor",
                "a careers advice centre",
                "discuss your job options: describe your experience and interests, and ask for "
                    + "advice on your CV"),
            ("trip_with_friend", "Planning a Trip Together",
                "an easy-going friend with strong opinions",
                "a video call to plan a weekend trip",
                "agree on a destination, transport and budget, and politely handle a disagreement "
                    + "about the plan"),
            ("mobile_plan", "Choosing a Phone Plan",
                "a persuasive mobile operator sales assistant",
                "a mobile operator's service centre",
                "choose a phone plan: compare two tariffs, ask about hidden fees, and politely "
                    + "refuse the extras you do not need"));

        // B2 - arguing a case and negotiating outcomes: work, money, disputes.
        AddLevel(
            CefrLevel.B2,
            ("salary_negotiation", "Negotiating a Salary",
                "a firm but fair hiring manager",
                "a salary discussion after a job offer",
                "negotiate your salary and conditions: justify the number you ask for, discuss "
                    + "benefits, and reach an agreement"),
            ("competency_interview", "Competency Interview",
                "a demanding senior interviewer",
                "a competency-based interview for a professional role",
                "answer behavioural questions with concrete examples from your experience, and ask "
                    + "insightful questions about the role"),
            ("performance_review", "Performance Review",
                "a direct but supportive line manager",
                "your annual performance review meeting",
                "present your achievements, respond calmly to criticism, and agree development "
                    + "goals for next year"),
            ("lease_negotiation", "Negotiating a Lease",
                "an experienced letting agent",
                "a meeting about an apartment lease",
                "negotiate the rent and contract length, ask for changes to the contract, and "
                    + "clarify who pays for repairs"),
            ("bank_loan", "Applying for a Loan",
                "a meticulous bank loan officer",
                "a loan application meeting at a bank",
                "apply for a loan: explain what it is for, discuss the rate and repayment terms, "
                    + "and answer questions about your income"),
            ("visa_interview", "Visa Interview",
                "a formal, unhurried consular officer",
                "a visa interview window at an embassy",
                "answer questions about your trip clearly and consistently, and provide the facts "
                    + "the officer asks for with confidence"),
            ("pitching_an_idea", "Pitching an Idea at Work",
                "a skeptical team lead",
                "a weekly team meeting",
                "present your improvement idea, field objections with evidence, and win support "
                    + "to try it"),
            ("complaint_escalation", "Escalating a Complaint",
                "a defensive customer-service supervisor",
                "an escalated phone call about an unresolved complaint",
                "summarize the history of the problem, stay firm but polite, and negotiate "
                    + "appropriate compensation"),
            ("networking_event", "At a Networking Event",
                "a well-connected industry professional",
                "a professional networking evening",
                "introduce yourself professionally, describe your work in an engaging way, and "
                    + "agree to stay in contact"),
            ("medical_results", "Discussing Test Results",
                "a candid, thorough doctor",
                "a follow-up consultation about your test results",
                "make sure you understand the results, ask about the options, and discuss the "
                    + "treatment plan critically"),
            ("cancelled_flight", "A Cancelled Flight",
                "an overloaded airline agent during a disruption",
                "an airline desk after your flight was cancelled",
                "get rebooked: state your rights calmly, request compensation, and compare the "
                    + "alternatives offered"),
            ("seminar_debate", "University Seminar",
                "a challenging university professor",
                "a university seminar discussion",
                "defend your viewpoint on the set topic, respond to counter-arguments, and concede "
                    + "points gracefully where needed"),
            ("buying_a_car", "Buying a Used Car",
                "a smooth-talking car salesman",
                "a used car showroom",
                "negotiate for a used car: ask about its history, resist pressure tactics, and "
                    + "negotiate the price down"),
            ("client_meeting", "A Difficult Client Meeting",
                "a results-focused client",
                "a project status meeting with your client",
                "explain a delay honestly, propose a recovery plan, and keep the client's "
                    + "confidence"),
            ("roommate_conflict", "A Flatmate Disagreement",
                "a frustrated flatmate",
                "a tense conversation in your shared kitchen",
                "resolve a disagreement about chores, bills and noise, and negotiate a compromise "
                    + "you can both accept"),
            ("deadline_pushback", "Pushing Back on a Deadline",
                "a pressured project manager",
                "a discussion about an unrealistic deadline",
                "push back on the deadline: explain the risks, and propose an alternative scope "
                    + "or date"),
            ("house_purchase", "Viewing a House to Buy",
                "a polished real-estate agent",
                "a house viewing with an estate agent",
                "look past the sales talk: ask about the condition, the neighbourhood and the "
                    + "price history, and start negotiating"),
            ("promotion_case", "Making a Case for Promotion",
                "a noncommittal HR business partner",
                "a meeting about your promotion prospects",
                "make your case for promotion with evidence, ask about the criteria, and agree "
                    + "a concrete timeline"),
            ("friendly_debate", "A Debate with a Friend",
                "an opinionated old friend",
                "a lively debate at a café",
                "debate a hot topic such as social media: disagree agreeably, support your "
                    + "opinions, and find common ground"),
            ("claim_dispute", "Disputing a Rejected Claim",
                "an evasive insurance claims adjuster",
                "a phone call disputing a rejected insurance claim",
                "challenge the rejection: cite the policy terms, counter the adjuster's reasons, "
                    + "and request a formal review"));

        // C1 - high-stakes professional scenes: leading, defending and persuading under pressure.
        AddLevel(
            CefrLevel.C1,
            ("panel_interview", "Panel Interview",
                "the stern chair of an interview panel",
                "a panel interview for a senior role",
                "handle rapid questions coming from several angles, keep your answers structured, "
                    + "and project calm leadership"),
            ("board_presentation", "Presenting to the Board",
                "a time-poor, numbers-driven board chair",
                "a boardroom strategy presentation",
                "present your strategy concisely, defend your numbers under scrutiny, and secure "
                    + "the board's approval"),
            ("investor_pitch", "Pitching to an Investor",
                "a sharp venture capital investor",
                "a startup pitch meeting",
                "pitch your business: the market, the model and your traction, and handle tough "
                    + "due-diligence questions"),
            ("contract_negotiation", "Contract Negotiation",
                "a seasoned procurement negotiator",
                "a contract negotiation meeting",
                "negotiate the terms: price, liability and timelines, trading concessions without "
                    + "giving away your position"),
            ("media_crisis_interview", "Crisis Media Interview",
                "a probing television journalist",
                "a live interview during a company crisis",
                "communicate under pressure: acknowledge the problem, bridge to your key messages, "
                    + "and avoid the traps in loaded questions"),
            ("conference_qna", "Conference Q&A",
                "a critical academic in the audience",
                "the question-and-answer session after your conference talk",
                "defend your methods and conclusions, and concede limitations gracefully without "
                    + "undermining your work"),
            ("lawyer_consultation", "Consulting a Lawyer",
                "a precise commercial lawyer",
                "a legal consultation about a business dispute",
                "explain the dispute accurately, make sure you understand your options, risks and "
                    + "costs, and decide how to proceed"),
            ("policy_debate", "Policy Debate",
                "a well-briefed debating opponent",
                "a structured debate on a public policy question",
                "argue your position with evidence, rebut your opponent's strongest points, and "
                    + "summarize persuasively"),
            ("partnership_negotiation", "Negotiating a Partnership",
                "a cautious potential business partner",
                "a business dinner to negotiate a partnership",
                "align your interests, propose a structure for the partnership, and handle the "
                    + "sensitive questions about money and control"),
            ("giving_feedback", "Giving Difficult Feedback",
                "a defensive underperforming employee",
                "a one-to-one performance conversation in which the learner is the manager",
                "deliver hard feedback with specific examples and empathy, and agree a concrete "
                    + "improvement plan"),
            ("diplomatic_reception", "A Formal Reception",
                "a foreign diplomat at a reception",
                "a formal diplomatic reception",
                "sustain polished small talk, navigate culturally sensitive subjects tactfully, "
                    + "and build genuine rapport"),
            ("medical_second_opinion", "Seeking a Second Opinion",
                "a specialist consultant physician",
                "a second-opinion consultation about a serious diagnosis",
                "discuss the diagnosis critically: the evidence behind it, the alternatives, and "
                    + "the trade-offs of each treatment"),
            ("mediation_session", "A Mediation Session",
                "a neutral professional mediator",
                "a mediation between you and a business partner",
                "state your grievances constructively, respond to the other side's version, and "
                    + "move the discussion toward a settlement"),
            ("thesis_defense", "Defending Your Thesis",
                "a rigorous external examiner",
                "your thesis defence",
                "justify your research choices, handle hostile questioning, and defend the "
                    + "contribution of your work"),
            ("press_briefing", "A Press Briefing",
                "an insistent lead reporter",
                "a product-launch press briefing",
                "deliver your key messages, handle skeptical follow-up questions, and stay "
                    + "on the record without misspeaking"),
            ("supplier_negotiation", "Negotiating with a Supplier",
                "a dominant key supplier",
                "an annual supplier negotiation",
                "rebalance the terms: use your volumes and alternatives as leverage, and secure "
                    + "price protection"),
            ("expert_panel", "On an Expert Panel",
                "a provocative panel moderator",
                "a live expert panel discussion",
                "give crisp expert answers, and disagree with fellow panellists constructively "
                    + "and memorably"),
            ("key_client_rescue", "Rescuing a Key Account",
                "an influential, furious key client",
                "an emergency meeting with your biggest client",
                "de-escalate the situation, take responsibility without over-conceding, and keep "
                    + "the account"),
            ("town_hall", "Leading a Town Hall",
                "a skeptical employee at a company town hall",
                "a company town hall where the learner is announcing a change",
                "announce an unpopular change, answer pointed questions honestly, and keep the "
                    + "team's trust"),
            ("radio_interview", "Live Radio Interview",
                "a witty radio talk show host",
                "a live radio interview about your field",
                "explain complex ideas accessibly, and handle humour and interruptions without "
                    + "losing your thread"));

        // C2 - mastery scenes: rhetoric, negotiation and precision at the highest stakes.
        AddLevel(
            CefrLevel.C2,
            ("keynote_qna", "Keynote Q&A",
                "a distinguished, exacting host",
                "the question-and-answer session after your keynote speech",
                "field philosophical, hostile and rambling questions alike with precision, wit "
                    + "and grace"),
            ("live_tv_debate", "Live TV Debate",
                "a formidable debating opponent",
                "a prime-time television debate",
                "win over the audience: deploy rhetoric deliberately, rebut sharply, and keep "
                    + "composure under personal attack"),
            ("parliamentary_hearing", "Parliamentary Hearing",
                "a relentless committee chair",
                "a parliamentary committee hearing",
                "testify: give precise answers, correct mischaracterizations on the record, and "
                    + "protect confidential matters lawfully"),
            ("summit_negotiation", "Summit Negotiation",
                "a masterful opposing chief negotiator",
                "an international summit negotiation",
                "negotiate a multilateral agreement: build alliances, manage your red lines, and "
                    + "shape the final wording"),
            ("crisis_press_conference", "Crisis Press Conference",
                "an aggressive lead correspondent",
                "an emergency press conference during a scandal",
                "manage the scandal: apologize credibly, define the narrative, and refuse the "
                    + "traps without appearing evasive"),
            ("philosophy_seminar", "Philosophy Seminar",
                "a Socratic philosophy professor",
                "a graduate philosophy seminar",
                "sustain a rigorous argument about free will and ethics, expose fallacies, and "
                    + "refine your position under questioning"),
            ("expert_witness", "Expert Witness",
                "a hostile cross-examining barrister",
                "the witness stand in a court case",
                "give expert testimony: absolute precision under cross-examination, and refuse "
                    + "to let your words be twisted"),
            ("emergency_board_meeting", "Emergency Board Meeting",
                "the chairman of a divided, alarmed board",
                "an emergency board meeting during a corporate crisis",
                "lead the room through the crisis: lay out the options, decide under uncertainty, "
                    + "and unite the factions"),
            ("diplomatic_negotiation", "Diplomatic Negotiation",
                "a subtle career ambassador",
                "a bilateral diplomatic negotiation",
                "negotiate a sensitive accord: read the subtext, use constructive ambiguity, and "
                    + "protect your country's interests"),
            ("adversarial_interview", "Adversarial Interview",
                "a famously combative interviewer",
                "a long-form adversarial interview",
                "handle selective quoting, interruptions and gotcha questions, and still land "
                    + "your argument"),
            ("peer_review_defense", "Defending Peer Review",
                "an eminent, dismissive reviewer",
                "a face-to-face discussion of your paper's harsh peer review",
                "defend your methodology against expert critique, and negotiate the revisions "
                    + "without surrendering the thesis"),
            ("merger_negotiation", "Merger Negotiation",
                "a hard-nosed opposing chief executive",
                "a merger negotiation between two companies",
                "negotiate valuation, control and culture, and keep the deal alive through "
                    + "the impasses"),
            ("policy_advisory", "Advising on Policy",
                "a demanding chief advisor to a minister",
                "a government policy consultation",
                "advise on a complex reform: synthesize the evidence, quantify the trade-offs, "
                    + "and defend your recommendation"),
            ("literary_panel", "Literary Festival Panel",
                "an erudite literary critic",
                "a panel discussion at a literary festival",
                "debate interpretation, style and the canon with wit, depth and quotable "
                    + "precision"),
            ("ethics_committee", "Ethics Committee Hearing",
                "a probing ethics committee chair",
                "a professional ethics committee hearing",
                "argue a nuanced ethics case: precedents, principles and mitigation, without "
                    + "resorting to platitudes"),
            ("final_investment_round", "Final Investment Round",
                "the managing partner of a legendary fund",
                "the final partner meeting for a major investment",
                "defend your valuation and vision against expert skepticism, and negotiate the "
                    + "term sheet"),
            ("moderating_a_panel", "Moderating a Heated Panel",
                "a combative celebrity expert",
                "a live panel that is spiralling out of control, which the learner is moderating",
                "moderate: enforce balance, defuse hostility elegantly, and extract substance "
                    + "from the noise"),
            ("ambassador_briefing", "Briefing an Ambassador",
                "a newly appointed, incisive ambassador",
                "a high-level briefing before a summit",
                "brief concisely on a volatile situation, and field rapid strategic questions "
                    + "without notes"),
            ("labor_negotiation", "Labour Dispute Negotiation",
                "a battle-hardened union leader",
                "a labour dispute negotiation on the eve of a strike",
                "avert the strike: negotiate pay and conditions, and manage the brinkmanship "
                    + "on both sides"),
            ("legacy_interview", "A Career Retrospective",
                "a thoughtful, well-prepared biographer",
                "a career-retrospective interview",
                "reflect eloquently on your failures, ethics and legacy, favouring nuance "
                    + "over cliché"));

        return scenarios;
    }
}
