<!-- Improved compatibility of back to top link: See: https://github.com/othneildrew/Best-README-Template/pull/73 -->

<a id="readme-top"></a>

<!--
*** Thanks for checking out the Best-README-Template. If you have a suggestion
*** that would make this better, please fork the repo and create a pull request
*** or simply open an issue with the tag "enhancement".
*** Don't forget to give the project a star!
*** Thanks again! Now go create something AMAZING! :D
-->

<!-- PROJECT SHIELDS -->
<!--
*** I'm using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links
-->
<!-- PROJECT LOGO -->

<br />
<div align="center">
  <a href="https://github.com/TheArkhamKnight781526/DoricoRemoteServer">
    <img src="dorico-logo.svg" alt="Logo" width="222" height="56">
  </a>

<h3 align="center">Dorico Remote (Server)</h3>

  <p align="center">
    Server for Dorico Remote Stream Deck Plugin
    <br />
    ·
    <a href="https://github.com/TheArkhamKnight781526/DoricoRemoteServer/issues/new?labels=bug&template=bug-report---.md">Report Bug</a>
    ·
    <a href="https://github.com/TheArkhamKnight781526/DoricoRemoteServer/issues/new?labels=enhancement&template=feature-request---.md">Request Feature</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#compiling-the-program">Compiling</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

![Dorico Remote StreamDeck Plugin][product-screenshot]

<!-- COMPILING -->

## Compiling the Program

### Prerequisites

This project is not much help without the accompanying Stream Deck plugin - which you can get [here](https://github.com/TheArkhamKnight781526/DoricoRemoteClient). However, if you wish to use it without the Stream Deck plugin, it can be done - see [below](example.com).

You will also need to have the .NET SDK installed - see the instructions on the [Microsoft Website](https://learn.microsoft.com/en-us/dotnet/core/install/) for details on how to do this.

### Installation

1. First, clone the repository.

2. Secondly, run `dotnet restore` to install all required packages

3. Finally, run `dotnet run` to debug locally, or `dotnet publish "Dorico Remote.generated.sln"` to build the executable.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- USAGE EXAMPLES -->

## Usage

### Changing the Port

By default, the server runs on port 5608 - if you wish to change it, navigate to your home directory, find a folder named `.dorico_remote/` (if it doesn't exist, you can create it), create a file named `.port`, and simply type the port you wish to use. Both the Stream Deck Plugin and Server will use this port upon restarting.

### Running the Executable as a Service

This program is designed to be run as a service (after all, it would be tedious to have to run it manually every time) - instructions for setting this up can be found below: (Instructions for MacOS are coming soon, but anyone witha a fair knowledge of launchctl should be able to get it working.)

#### Windows:

1. To add this as a service, ensure you have an executable file ready to add. You can get this from the releases page, or by building the software yourself.

2. Ensure the executable is located somewhere it is unlikely to be deleted by accident!

3. Open an elevated Powershell (search for Powershell, right click, and press `Run as Administrator`)

4. Run the following command: `sc.exe create "Dorico Remote Server" binpath= "path_to_executable"` (Note: you **must** leave a space after binpath) - you should see the message `[SC] CreateService SUCCESS`

5. Now run: `sc.exe start "Dorico Remote Server"`

6. Open Services from Windows Search, and scroll down until you find "Dorico Remote Server", and ensure that it is running - a screenshot is attached below.

![Hello][services-screenshot]

_Note: This program will **not** start/stop itself automatically when Dorico is opened or closed (this might be done in future, but at the moment is not available). Therefore, you will either need to start/stop it manually when you open/close Dorico (simply open Services, right click and click start/stop), or set it to run automatically at start up (right click, click on properties, and change `Startup Type` to `Automatic`)_

### Using the Server without a Stream Deck (Experienced Users Only)

Whilst the program was designed with a Stream Deck in mind, and built alongside a Stream Deck plugin, there is technically nothing stopping you from using it with any device (hardware/software) you like, providing you can use it to trigger HTTP requests.

The code from the [Stream Deck plugin](https://github.com/TheArkhamKnight781526/DoricoRemoteClient) contains all of the details for the HTTP requests, but just as an example, for sending a normal Dorico command, your request should look like this:

```ts
fetch(`http://localhost:${your_port_here}`, {
  method: "POST",
  body: JSON.stringify({
    command: dorico_command_here,
    parameterNames: command_parameters_here,
    parameterValues: command_values_here,
  }),
  headers: {
    "Content-Type": "application/json; charset=UTF-8",
  },
});
```

The example shown is in TypeScript, but you can make a similar request from the programming language of your choice. In terms of required parameters:

- `Port: String (or Integer in Dynamically Typed Languages)` - Port Number
- `Command: String` - A Dorico command: details for finding Dorico commands can be found [here](example.com)
- `Parameter Names: String[]` - An array of parameter names in order ([name1, name2, name3])
- `Parameter Values: String[]` - An array of parameter values in order ([value1, value2, value3])

As previously stated, non Stream Deck solutions are not the focus of this project, but if you run into serious problems, feel free to open an issue.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ROADMAP -->

## Roadmap

See the [open issues](https://github.com/TheArkhamKnight781526/DoricoRemoteServer/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTRIBUTING -->

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- LICENSE -->

## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTACT -->

## Contact

Feel free to open an issue if you encounter a bug in the program, or if there is a feature missing that you would like to see added.

(Note: Functionality is currently limited by the Dorico API, so some requests may not be possible)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ACKNOWLEDGMENTS -->

## Acknowledgments

- [Dorico.Net](https://github.com/scott-janssens/Dorico.Net)
- [README Template](https://github.com/othneildrew/Best-README-Template/tree/main)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[product-screenshot]: ./screenshot.png
[services-screenshot]: ./services-screenshot.png
