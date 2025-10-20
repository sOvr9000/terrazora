
using System;

public class NameGenerator {

	public static readonly string[] adjectives = new string[]
	{
		"Clever",
		"Industrious",
		"Tireless",
		"Efficient",
		"Rusted",
		"Brilliant",
		"Silent",
		"Fiery",
		"Quantum",
		"Shining",
		"Ironclad",
		"Obsidian",
		"Cunning",
		"Steadfast",
		"Binary",
		"Forgotten",
		"Voltaic",
		"Prime",
		"Arcane",
		"Diligent",
		"Clumsy",
		"Ancient",
		"Aged",
		"Archaic",
		"Dwelling",
		"Elaborate",
		"Greatest",
		"Fastest",
		"Old",
		"Glorious",
		"Crazy",
		"Primordial",
		"Simple",
	};

	public static readonly string[] nouns = new string[]
	{
		"Engineer",
		"Machinist",
		"Constructor",
		"Alchemist",
		"Innovator",
		"Artisan",
		"Fabricator",
		"Miner",
		"Architect",
		"Forger",
		"Technician",
		"Pioneer",
		"Inventor",
		"Mechanic",
		"Tinkerer",
		"Operative",
		"Overseer",
		"Builder",
		"Prospector",
		"Synthesist",
		"Strategist",
		"Hand Crank",
		"Screw",
		"Cogwheel",
		"Gear",
		"Piston",
		"Flywheel",
		"Pulley",
		"Lever",
		"Axle",
		"Rod",
		"Bearing",
		"Spring",
		"Valve",
		"Rivet",
		"Chain",
		"Gearbox",
		"Switch",
		"Circuit",
		"Boiler",
		"Turbine",
		"Conveyor",
	};

	public static readonly string[] places = new string[]
	{
		"The Mines",
		"The Foundry",
		"The Wastes",
		"The Core",
		"The Workshop",
		"The Tunnels",
		"The Ruins",
		"The Assembly",
		"The Engineworks",
		"The Outpost",
		"The Nexus",
		"The Depths",
		"The Power Plant",
		"The Quarry",
		"The Tower",
		"The Forge",
		"The District",
		"The Reactor",
		"The Factory",
		"Tomorrow",
		"Yesterday",
		"The Future",
		"The Unknown",
		"The Conveyors",
		"The Afterlife",
		"Pandemonium",
		"Chaos",
		"Greatness",
		"Simplicity",
		"Logistics",
		"Innovation",
		"Equilibrium",
		"Efficiency",
		"Entropy",
		"Perfection",
		"Progress",
		"Genesis",
		"Continuum",
		"Obsolescence",
		"Industry",
		"Precision",
		"Automation",
		"Control",
		"Ascension",
		"Momentum",
		"Expansion",
		"Reconstruction",
		"Love", // lmao ... The Fastest Rod of Love
	};

	private Random rng;

	public NameGenerator(Random rng) {
		this.rng = rng;
	}

	public string GenerateName() {
		string adjective = adjectives[rng.Next(adjectives.Length)];
		string noun = nouns[rng.Next(nouns.Length)];
		string place = places[rng.Next(places.Length)];

		return $"The {adjective} {noun} of {place}";
	}

}
