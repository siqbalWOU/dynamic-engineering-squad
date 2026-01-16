# Smart Pantry App Idea ("Savor")
## Key Questions
### What is it?

The application is a mobile-responsive "Smart Kitchen" assistant that helps users track what food they currently own, reduces food waste, and simplifies meal planning.  

- The Core Loop: Users scan or log groceries as they enter the home to create a "digital pantry."

- AI Integration: An AI (OpenAI/Gemini) acts as an intelligent chef. Users can hit a "What can I cook?" button, and the AI will generate recipes based only on the ingredients currently in their digital pantry, prioritizing items that are about to expire.

- Preferences: Users can set dietary preferences (e.g., "Vegan," "Gluten-Free") which the AI respects when generating ideas.

- Tech Stack: * Google Cloud Vision or OpenFoodFacts API is used to scan barcodes to instantly populate the pantry without manual typing.

- SendGrid or Twilio APIs are used to send automated alerts when food is approaching its expiration date.

### What is the purpose?

The purpose of the app is to solve the "disconnect" between buying food and eating it.  

- Financial: It aims to save users money by ensuring they use the groceries they've already purchased before buying more.

- Environmental: It reduces environmental impact by minimizing household food waste.

- Mental: It solves "decision fatigue"—the stress of not knowing what to cook—by automating the creative process of meal planning based on existing inventory.

### Why are you proposing this topic?

- Food waste is a massive economic and environmental issue; the average US household wastes nearly 30% of the food they buy (USDA). Most current solutions are just static recipe books that don't know what the user actually has.

- I am proposing this to bridge that gap using modern AI. By making inventory tracking frictionless (barcode scanning) and the output highly valuable (custom AI recipes that save money), we can change user behavior from wasteful to efficient.

### Are there any competitors or existing applications that do this? If so, list them and explain why your topic is better or still worth doing.

- Yes. Apps like SuperCook and MyFridgeFood allow users to select ingredients to find recipes. However, these are often manual "check-box" tools that rely on static databases of pre-written recipes.

Why our topic is better:  

- Generative vs. Static: Our application uses Generative AI rather than a static database search. This means it can invent new recipes or creative "hacks" to use up weird combinations of leftovers that wouldn't exist in a standard recipe database.

- Lifecycle Tracking: Most competitors lack the "lifecycle" component—they don't track expiration dates or alert you when food is going bad. Our app closes the loop by managing the food from purchase to consumption, not just the cooking step.

### Vision Statement

This project is an AI-powered ASP.NET inventory management application defined by three core features: barcode-scanning for effortless intake, AI-driven recipe generation that adapts to available stock, and automated expiration tracking to prevent waste.

The purpose of this platform is to eliminate household food waste and decision fatigue by transforming a user's static pantry into a dynamic menu. While competitors like SuperCook exist, they rely on manual input and static recipe databases; our application improves upon this by using Generative AI to create custom culinary solutions for unique ingredient combinations and proactive notifications, ensuring users save money and eat everything they buy.