# Simple Template

This template serves as a foundation for projects following the **Clean Cut Architecture (CCA)** principles. It includes:

1. A well-structure folder organization based on CCA.
2. An example use case: **Check Pulse**, demonstrating the implementation of key architectural concepts.

## Features

- **Screaming Intent:** The folder structure highlights the purpose of each component, emphasizing clarity and intent.
- **Cohesion and Isolation:** Components are tightly focused on their specific responsibilities and interact only where necessary.
- **Extendable Template:** Easily adapt this template to fit your business needs and domain requirements.
- **Intentional Error Design:** Error scenarios are designed as part of the use case logic.

## Example Use Case: Check Pulse

The `Check Pulse` use case showcases the core principles of CCA while serving as an optional health-check mechanism.

- **Purpose:** Validates that the application is operational and running correctly.
- **Input:** Accepts an optional string for testing purposes.
- **Output:** Returns a success or failure result.
- **Endpoint:** Exposed via `GET /check-pulse` for integration testing or operational monitoring.

## How to Use

**1. Understand the Template Structure:**
Explore the folder organization to understand where use cases and other components reside, and how they come together.

**2. Replace Example Use Case:**
Replace `CheckPulseUseCase` with your own business-specific use cases, following the same design principles.

**3. Implement Your Use Cases:**
Add your business-specific logic while ensuring it adheres to the principles of cohesion and isolation.

**4. Extend Infrastructure:**
Customize the infrastructure layer to fit your project’s dependencies, databases, and external systems.

## Optional: Using It as a dotnet Template

Transform this project into a reusable .NET template to accelerate your development workflow:

**1. Pack the template**
Open your terminal and run:

`dotnet new install CleanCutArchitectureTemplate -force`

This will install the solution structure as a template for you to use from the dotnet CLI or the Wizard.

**2. Use the template**
Open your terminal on the location you want to start your new solution that uses CCA, and run (replace YourSolutionName with the actual name of your solution):

`dotnet new cca -n <YourSolutionName>`

You can also use the more verbose command:

`dotnet new clean-cut-architecture -n <YourSolutionName>`
