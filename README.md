# Cosas
Simple list with things and the place where they are.

## Install Prerequisites
Download and install Visual Studio Community and Xamarin Platform from [Xamarin download page](https://www.xamarin.com/download).

## Create Firebase project
Start a free Plan Spark on [Google Firebase](https://firebase.google.com). Create a new project and add an android app with this package name (com.pove.cosas). You can use your own package name.

## Setup Firebase variables
Open `Cosas/MainActivity.cs` and set your Firebase variables:

```c#
// Firebase variables, you can set them up by code
private string ApplicationId = "";
private string ApiKey = "";
private string DatabaseUrl = "";
```
If you leave them empty, you will be asked for them on app start.
