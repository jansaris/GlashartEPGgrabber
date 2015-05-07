﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using log4net;

namespace GlashartLibrary.Helpers
{
    public sealed class XmlTvHelper
    {
        private static XmlDocument xml;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(XmlTvHelper));

        /// <summary>
        /// Generates the XMLtv file
        /// </summary>
        /// <param name="epgChannels">The epg channels.</param>
        /// <param name="channels">The channels.</param>
        /// <param name="fileName">Name of the file.</param>
        public static void GenerateXmlTv(List<EpgChannel> epgChannels, List<Channel> channels, string fileName)
        {
            xml = new XmlDocument();
            GenerateRoot();
            GenerateChannels(epgChannels, channels);
            GeneratePrograms(epgChannels, channels);

            //Save xml
            xml.InsertBefore(xml.CreateXmlDeclaration("1.0", "UTF-8", null), xml.DocumentElement);
            xml.Save(fileName);
        }

        /// <summary>
        /// Generates the root node
        /// </summary>
        /// <returns></returns>
        private static XmlNode GenerateRoot()
        {
            var node = AppendNode(xml, "tv");
            AppendAttribute(node, "generator-info-name", "GlashartEPGgrabber (by Dennieku & jansaris)");
            return node;
        }

        /// <summary>
        /// Generates the channels.
        /// </summary>
        /// <param name="channels">The channels.</param>
        private static void GenerateChannels(List<EpgChannel> epgChannels, List<Channel> channels)
        {
            var root = xml.DocumentElement;
            //Loop through the channels
            foreach (var channel in channels)
            {
                //Get the epg channel
                var epgChannel = epgChannels.FirstOrDefault(c => c.Channel.Equals(channel.Key, StringComparison.InvariantCultureIgnoreCase));
                if (epgChannel != null)
                {
                    var channelNode = AppendNode(root, "channel");
                    AppendAttribute(channelNode, "id", channel.Name);
                    var displayNode = AppendNode(channelNode, "display-name", channel.Name);
                    AppendAttribute(displayNode, "lang", "nl"); //just setting everything to NL
                }
            }
        }

        /// <summary>
        /// Generates the programs.
        /// </summary>
        /// <param name="epgChannels">The epg channels.</param>
        /// <param name="channels">The channels.</param>
        private static void GeneratePrograms(List<EpgChannel> epgChannels, List<Channel> channels)
        {
            var root = xml.DocumentElement;

            //Loop through the channels
            foreach (var channel in channels)
            {
                //Get the epg channel
                var epgChannel = epgChannels.FirstOrDefault(c => c.Channel.Equals(channel.Key, StringComparison.InvariantCultureIgnoreCase));
                if(epgChannel == null) continue;
                //Loop through the programs
                foreach (var prog in epgChannel.Programs)
                {
                    try
                    {
                        //Create the xml node
                        var progNode = AppendNode(root, "programme");
                        AppendAttribute(progNode, "start", prog.StartString);
                        AppendAttribute(progNode, "stop", prog.EndString);
                        AppendAttribute(progNode, "channel", channel.Name);
                        var titleNode = AppendNode(progNode, "title", prog.Name);
                        AppendAttribute(titleNode, "lang", "nl");
                        if (!string.IsNullOrWhiteSpace(prog.Description))
                        {
                            var descNode = AppendNode(progNode, "desc", prog.Description);
                            AppendAttribute(descNode, "lang", "nl");
                        }
                        if (prog.Genres != null && prog.Genres.Any())
                        {
                            foreach (var genre in prog.Genres)
                            {
                                var categoryNode = AppendNode(progNode, "category", genre.Genre);
                                AppendAttribute(categoryNode, "lang", genre.Language);
                            }
                        }
                        //TODO: add other epg info
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed on prog {0}", prog.Id);
                    }
                }
            }
        }


        /// <summary>
        /// Appends the node.
        /// </summary>
        /// <param name="parent">The parent node</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private static XmlNode AppendNode(XmlNode parent, string name, string innerText = null)
        {
            XmlNode node = xml.CreateElement(name);
            parent.AppendChild(node);
            if (!string.IsNullOrWhiteSpace(innerText))
                node.InnerText = innerText;
            return node;
        }

        /// <summary>
        /// Appends the attribute.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static XmlAttribute AppendAttribute(XmlNode node, string name, string value)
        {
            var attr = xml.CreateAttribute(name);
            attr.Value = value;
            node.Attributes.Append(attr);
            return attr;
        }
    }
}
