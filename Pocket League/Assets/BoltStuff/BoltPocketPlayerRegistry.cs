using System.Collections.Generic;

public static class BoltPocketPlayerRegistry
{
    public static int numPlayers = 0;
    public static bool isServer = false;

    // keeps a list of all the players
    static List<BoltPocketPlayer> players = new List<BoltPocketPlayer>();

    // create a player for a connection
    // note: connection can be null
    static BoltPocketPlayer CreatePlayer(BoltConnection connection)
    {
        if(numPlayers >= 2)
        {
            return null;
        }

        BoltPocketPlayer player;

        // create a new player object, assign the connection property
        // of the object to the connection was passed in
        player = new BoltPocketPlayer();
        player.connection = connection;

        // if we have a connection, assign this player
        // as the user data for the connection so that we
        // always have an easy way to get the player object
        // for a connection
        if (player.connection != null)
        {
            player.connection.UserData = player;
        }

        // add to list of all players
        players.Add(player);
        numPlayers = players.Count;

        return player;
    }

    // this simply returns the 'players' list cast to
    // an IEnumerable<T> so that we hide the ability
    // to modify the player list from the outside.
    public static IEnumerable<BoltPocketPlayer> AllPlayers
    {
        get { return players; }
    }

    // finds the server player by checking the
    // .IsServer property for every player object.
    public static BoltPocketPlayer ServerPlayer
    {
        get { return players.Find(player => player.IsServer); }
    }

    // utility function which creates a server player
    public static BoltPocketPlayer CreateServerPlayer()
    {
        return CreatePlayer(null);
    }

    // utility that creates a client player object.
    public static BoltPocketPlayer CreateClientPlayer(BoltConnection connection)
    {
        return CreatePlayer(connection);
    }

    // utility function which lets us pass in a
    // BoltConnection object (even a null) and have
    // it return the proper player object for it.
    public static BoltPocketPlayer GetPocketPlayer(BoltConnection connection)
    {
        if (connection == null)
        {
            return ServerPlayer;
        }

        return (BoltPocketPlayer)connection.UserData;
    }
}
