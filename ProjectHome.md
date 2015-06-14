Create, edit, and get quizzed on your [Quizlet](http://quizlet.com/) flashcards. Store them offline and practice them in your secret underground bunker.

This is a flashcards/vocabulary learning application for Windows Phone 7. It is written in C# with Silverlight, and uses http://quizlet.com as its back end. In order to build this you will need to add create a Quizlet API key and add relevant ClientID and SecretKey properties to the QuizletAPI class (preferably using partial classes).

Uses a slight modification of [the C# version of BouncyCastle](http://www.bouncycastle.org/csharp/) for the HTTPS communication with the Quizlet API; see the [BouncyCastle licence](http://www.bouncycastle.org/csharp/licence.html) for its licence.