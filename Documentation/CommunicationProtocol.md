# Communication Protocol

This document describes the communication protocol between Team 525's custom TCP
dashboard server (on robot) and dashboard client (on the Driver's Station
laptop).

## Table of Contents

## Important Concepts

This protocol refers to several concepts intrinsic to the execution of the TCP
client and server. This section outlines what those concepts mean.

### Robot Values

A "Robot Value" represents the value or status of a piece of information on the
robot. This can include sensor values, motor outputs, odometry positions, or
other numeric/boolean information.

### Dashboard Values

A "Dashboard Value" represents a configurable setting on the robot that is
controlled and managed by the client. This can include sensor zero offsets,
range of motion limits, or other parameters that changes how the robot code
behaves.

### Events

An "Event" is an occurence that the robot detects takes place. Events have 2
severity levels: `info` and `error`.

### Buttons

A "Button" is a button on the client UI that, when pressed, the client
continuously alerts the server that the button is pressed until the user
releases it. The robot can use this information to take action, but the action
is determined by the robot, not by the client. This means it's possible not all
buttons in the client UI are "Buttons" in this context.

## Overall Behavior

On startup, the dashboard server (the robot) will continuously try to accept TCP
connections from the client. The server will only reply to the client when a
command is received and will tailor its response to the client based on the
received message.

The supported message types are shown below. If the server receives a message
that it doesn't recognize, it will respond with the phrase `NACK` (indicating a
negative acknowledgement).

### QUERY

This command allows the client to ask the server for identifying information on
the Robot Values, Dashboard Values, and Buttons the server is aware of. This
identifying information is then used by the client during other commands.

Messages from the client are of the format: `QUERY:<X><N>`

where:
- `<X>` is the type of item being queried (i.e. `R` for Robot Value, `D` for
  Dashboard Value, or `B` for Button)
- `<N>` is the index of the item being queried (0-indexed)

For example, if a client wants to ask "what is the identifier for the 5th
Dashboard Value?", it will send the command: `QUERY:D4`.

The server will respond with a string the client can use internally to identify
the Dashboard Value. For example, it may respond with:
`QUERY:D4|Elevator Zero Offset`.

If the client asks for an index that is outside the range of what the server is
aware of, the server will respond with a NACK scoped to the query command.

The corresponding NACK for the above example would be: `QUERY:D4|NACK`

### GET

This command allows the client to ask the server for Robot Values using the
indices retrived from prior `QUERY` commands.

Messages from the client are of the format: `GET:<N>...`

where:
- `<N>` is the index of the Robot Value to query.

Multiple Robot Values can be requested at once; indices are provided as a
comma-separated list.

For example, a client may ask for the 3rd Robot Value with the following
command: `GET:2`. Similarly, a client may ask for the 1st, 4th, and 7th Robot
Values with the following command: `GET:0,3,6`

The server will respond by inserting the current value of the corresponding
Robot Value after its index in the command string. The format of the value
depends on the data type of the value:
- `int` values: the numeric integer value as a whole number, possibly preceeded
  by a negative sign
- floating point values: the numeric floating point number. The value will
  typically be rounded to 2 decimal places, but the server will choose different
  rounding if it's relevant to the data being transmitted
- `boolean` values: the lowercase string `true` or `false`
- enumerated values: the string representing the current enumeration value (e.g.
  `extended` or `low gear`)

An example server response to the above example command might look like:
`GET:2|extended`. A response containing multiple values may look like:
`GET:0|false,3|3.14,6|7`

If the client asks for an index that is outside the range of what the server is
aware of, the server will respond with a NACK for that Robot Value only.

Example NACK responses might look like:
- `GET:2|NACK`
- `GET:0|4,3|NACK,6|true`

Although responses indicate the index of a Robot Value along with its value,
Robot Values are always returned to the client in the same order the client asks
for them.

### SET

This command allows the client to ask the server to update a Dashboard Value as
specified by the client using indices retrieved from prior `QUERY` commands.

Messages from the client are of the format: `SET:<N>|<V>...`

where:
- `<N>` is the index of the Dashboard Value to set the value of
- `<V>` is the value to assign to the Dashboard Value

Multiple Dashboard Values can be set at once; indices and their values are
provided as a comma-separated list. The format of the value depends on the data
type of the value:
- `int` values: the numeric integer value as a whole number, possibly preceeded
  by a negative sign
- floating point values: the numeric floating point number. The client will
  determine whether and how rounding is performed
- `boolean` values: the lowercase string `true` or `false`
- enumerated values: the string representing the desired enumeration value (e.g.
  `extended` or `low gear`)

For example, a client may set a Dashboard Value with the following command:
`SET:2|true`. Similarly, a client may set several Dashboard Values
simultaneously with the following command: `SET:1|7.32,4|low gear,5|9`

The server will respond by replacing the value of the setting with either the
string `ACK` (if the value is accepted) or `NACK` (if the value is rejected).

Example responses for the above example commands might look like:
- `SET:2|ACK`
- `SET:1|NACK,4|NACK,5|ACK`

### EVENT

This command allows the client to query the server for any events that have
occurred since the last time the client sent an `EVENT` command.

Messages from the client are of the format: `EVENT:`

There are no variable parameters provided with this command.

The server will respond with messages in the following format:
`EVENT:<S>|<M>...`

where:
- `<S>` is the severity of the event (e.g. `info` or `error`)
- `<M>` is the message for the event

Multiple Events can be returned from a single `EVENT` command. Events are
separated by commas.

For example, the server may respond with a single event like this:
`EVENT:info|Algae picked up!`. Similarly, the server may respond with multiple
events like this: `EVENT:error|Gyro disconnected!,info|Robot collision detected`

If the server has no new events since the last time the client sent an `EVENT`
command, the server will respond with `EVENT:`.

### BUTTON

This command allows the client to indicate to the server that a particular
Button on the client UI is being pressed using indices received from prior
`QUERY` commands.

The client will indicate when a Button is initially pressed, and will continue
to send messages to the server indicating the button is being held. Once a
Button is released, the client will send a final message indicating as such.

Messages indicating a Button is held should be transmitted every 250
milliseconds, and the server should assume a Button has been released if it has
not received a message from the client indicating as such within 500
milliseconds.

Messages are of the format: `BUTTON:<N>|<B>...`

where:
- `<N>` is the index of the Button
- `<B>` is the state of the Button as a boolean (`true` or `false`)

Multiple Buttons' states can be provided from the client in a single message. In
this case, Button states are provided as a comma-separated list.

The server will respond by replacing the state of each Button with either an
`ACK` or a `NACK`, depending on if the state for that particular index is
accepted or rejected.

The example below illustrates a scenario where a Button is pressed, held for
some time, and released. Messages transmitted by the client are prepended with
`C:`, and messages transmitted by the server are prepended with `S:`. These
identifiers are NOT actually transmitted, just the content that follows.

```
C: BUTTON:1|true
S: BUTTON:1|ACK
C: BUTTON:1|true
S: BUTTON:1|ACK
C: BUTTON:1|true
S: BUTTON:1|ACK
C: BUTTON:1|false
S: BUTTON:1|ACK
```

The example below illustrates an example where 1 Button is pressed, then
another is pressed, held, released, then the 1st Button is released:

```
C: BUTTON:1|true
S: BUTTON:1|ACK
C: BUTTON:1|true,2|true
S: BUTTON:1|ACK,2|ACK
C: BUTTON:1|true,2|true
S: BUTTON:1|ACK,2|ACK
C: BUTTON:1|true,2|false
S: BUTTON:1|ACK,2|ACK
C: BUTTON:1|true
S: BUTTON:1|ACK
C: BUTTON:1|false
S: BUTTON:1|ACK
```

### PING

This message conveys and requests no information, but is used to help the
client understand if it's connected to the server. This message is ONLY
transmitted if no other commands have been transmitted in the last 2 seconds.

The client's message is simply `PING`. The server's response is simply `PONG`.
