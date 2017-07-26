﻿using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.LevelGeneration;
using UnityEngine;

public class SearchAStar {
    private readonly Vector2 _end;
    private readonly PathHeuristic _heuristic;
    private readonly int[,] _searchMap;
    private readonly Vector2 _start;

    public SearchAStar(int[,] searchMap, Vector2 start, Vector2 end, PathHeuristic heuristic) {
        _searchMap = searchMap;
        _start = start;
        _end = end;
        _heuristic = heuristic;
    }

    public List<Edge> Search() {
        var startRecord = new NodeRecord();
        startRecord.location = _start;
        startRecord.connection = null;
        startRecord.costSoFar = 0;
        startRecord.estimatedTotalCost = _heuristic.estimate(_start);

        //TODO Replace with a priority queue
        var openList = new List<NodeRecord>();
        var closedList = new List<NodeRecord>();

        openList.Add(startRecord);

        NodeRecord current = null;
        while (openList.Count > 0) {
            //TODO: Get node with smallest estimate
            current = openList[0];

            //If we're at the goal, end early
            if (current.location.Equals(_end)) {
                break;
            }

            //Otherwise, get the connections
            var connections = getConnections(current);
            NodeRecord endNodeRecord;
            Vector2 endLoc;
            float endCost;
            float endHeuristic;
            foreach (var con in connections) {
                endLoc = new Vector2(con.destination.x, con.destination.y);
                endCost = current.costSoFar + con.cost;

                //If the node is closed, we may have to skip or remove from the closed list
                if (closedList.Any(conn => conn.location.Equals(endLoc))) {
                    endNodeRecord =
                        closedList.Single(locRec => locRec.location.Equals(endLoc)); //Retrieve the record we found
                    if (endNodeRecord.costSoFar <= endCost) {
                        //If this route isn't shorter, then skip.
                        continue;
                    }
                    //Otherwise, remove it from the closed list
                    closedList.Remove(endNodeRecord);
                    //Recalculate the heuristic. TODO: recalculate using old values
                    endHeuristic = _heuristic.estimate(endLoc);
                }
                else if (openList.Any(conn => conn.location.Equals(endLoc))) {
                    //Skip if the node is open and we haven't found a better route
                    endNodeRecord = openList.Single(locRec => locRec.location.Equals(endLoc));

                    if (endNodeRecord.costSoFar <= endCost) {
                        continue;
                    }
                    //Also might be wrong
                    endHeuristic = _heuristic.estimate(endLoc);
                }
                else {
                    //Otherwise, we're on an unvisited node that needs a new record
                    endNodeRecord = new NodeRecord();
                    endNodeRecord.location = endLoc;
                    endHeuristic = _heuristic.estimate(endLoc);
                }
                //If we reached this point, it means we need to update the node
                endNodeRecord.costSoFar = endCost;
                endNodeRecord.connection = con; //remember: we're iterating through the connections right now
                endNodeRecord.estimatedTotalCost = endCost + endHeuristic;

                if (!openList.Any(openConn => openConn.location.Equals(endLoc))) {
                    openList.Add(endNodeRecord);
                }
            }
            //Finished looking at the connections, move it to the closed list.
            openList.Remove(current);
            closedList.Add(current);
        }
        if (!current.location.Equals(_end)) {
            //We're out of nodes and haven't found the goal. No solution.
            return null;
        }
        //We found the path, time to compile a list of connections
        var outputList = new List<Edge>(20);

        while (!current.location.Equals(_start)) {
            outputList.Add(current.connection);
            current = current.connection.previousRecord;
        }
        outputList.Reverse();
        return outputList;
    }

    private List<Edge> getConnections(NodeRecord tileRecord) {
        var worldCoordinate = tileRecord.location;
        var worldX = (int) Mathf.Floor(worldCoordinate.x);
        var worldY = (int) Mathf.Floor(worldCoordinate.y);
        var retList = new List<Edge>(4);

        //TODO: Fix: When world generation is more robust, otherwise open tiles that have had an enemy or an item spawned on them will be considered blocked 
        if (_searchMap[worldX + 1, worldY] == (int) LevelDecoration.Floor) {
            retList.Add(new Edge(worldCoordinate, new Vector2(worldX + 1, worldY), tileRecord));
        }
        if (_searchMap[worldX + -1, worldY] == (int) LevelDecoration.Floor) {
            retList.Add(new Edge(worldCoordinate, new Vector2(worldX - 1, worldY), tileRecord));
        }
        if (_searchMap[worldX, worldY + 1] == (int) LevelDecoration.Floor) {
            retList.Add(new Edge(worldCoordinate, new Vector2(worldX, worldY + 1), tileRecord));
        }
        if (_searchMap[worldX, worldY - 1] == (int) LevelDecoration.Floor) {
            retList.Add(new Edge(worldCoordinate, new Vector2(worldX, worldY - 1), tileRecord));
        }
        return retList;
    }

    public class NodeRecord {
        public Edge connection;
        public float costSoFar;
        public float estimatedTotalCost;
        public Vector2 location;
    }

    public class Edge {
        public float cost = 1;
        public Vector2 destination;
        public Vector2 from;
        public NodeRecord previousRecord;

        public Edge(Vector2 from, Vector2 destination, NodeRecord previousRecord) {
            this.from = from;
            this.destination = destination;
            this.previousRecord = previousRecord;
        }
    }
}

public abstract class PathHeuristic {
    protected Vector2 _goalLocation;

    protected PathHeuristic() { }

    public PathHeuristic(Vector2 goalLocation) {
        _goalLocation = goalLocation;
    }

    public abstract float estimate(Vector2 startFrom);
}

public class ManhattenDistance : PathHeuristic {
    public override float estimate(Vector2 startFrom) {
        return startFrom.x + _goalLocation.x
               + startFrom.y + _goalLocation.y;
    }
}