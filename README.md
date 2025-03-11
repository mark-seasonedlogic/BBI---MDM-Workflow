# BBI - MDM Workflow
Change Log:

V1.1.0:

Refactoring Summary & SOLID Principles in Action
What We Did in This Refactoring
Implemented MVVM Architecture

Moved UI logic to MainViewModel.cs, keeping MainWindow.xaml.cs focused only on view-related concerns.
Introduced data binding (x:Bind) to remove direct UI event handling in code-behind.
Created GitRepositoryManager.cs

Encapsulated all Git operations (initialization, branching).
Ensured reusability and separation of concerns by keeping business logic separate from the UI.
Added RelayCommand.cs

Allowed command binding to buttons (CreateBranchCommand).
Followed dependency injection principles by passing commands dynamically.
Improved UI Layout

Structured MainWindow.xaml for better UX (clear labels, proper alignment).
Added status messages (StatusMessage in MainViewModel) to provide user feedback.
How We Followed SOLID Principles
Principle	How It Was Applied
Single Responsibility	Each class now has a clear, focused role: UI (MainWindow), ViewModel (MainViewModel), Git Logic (GitRepositoryManager), and Commands (RelayCommand).
Open/Closed	The system can support new Git actions (e.g., commits, diffs) by extending GitRepositoryManager, without modifying existing code.
Liskov Substitution	The RelayCommand can be replaced or extended with more advanced command-handling logic if needed.
Interface Segregation	MVVM architecture ensures that UI classes do not depend on Git operations directly.
Dependency Inversion	MainViewModel depends on an abstraction (GitRepositoryManager), making it testable and easily replaceable.

V1.2.0:
Added Features
-CommitChanges(string message) → Stages all files and commits changes.
-GetDiff() → Returns a string showing the difference between the last commit and working directory.
-CreateTag(string tagName, string message) → Tags the latest commit.
-GetCommitHistory() → Retrieves the last 10 commit messages.

