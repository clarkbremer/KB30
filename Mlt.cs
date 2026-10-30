using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace KB30
{
    public partial class MainWindow : Window
    {
        private string LongTimeString(double t)
        {
            int durationHours = (int)(t / 3600);
            int durationMins = (int)(t / 60);
            int durationSecs = (int)(t % 60);
            int durationMs = (int)((t - Math.Truncate(t)) * 1000);
            return durationHours.ToString("D2") + ":" + durationMins.ToString("D2") + ":" + durationSecs.ToString("D2") + "." + durationMs.ToString("D3");
        }
        private int fileExportMLTClick(StreamWriter outputFile)
        {
            int frame_height = 1440;  /* Hard coded to target resolution.  Must match shotcut settings */
            int frame_width = 2560;

            int filter_num = 0;
            int producer_num = 0;
            int tractor_num = 0;
            int transition_num = 0;
            int playlist_num = 0;
            double previous_slide_duration = 0;
            List<double> producer_lengths = new List<double> { };



            string preamble = $"""
                    <?xml version="1.0" standalone="no"?>
                    <mlt LC_NUMERIC="C" version="7.30.0" title="Shotcut version 25.03.29" producer="main_bin">
                      <profile description="PAL 4:3 DV or DVD" width="{frame_width}" height="{frame_height}" progressive="1" sample_aspect_num="1" sample_aspect_den="1" display_aspect_num="16" display_aspect_den="9" frame_rate_num="60" frame_rate_den="1" colorspace="709"/>
                      <playlist id="main_bin">
                        <property name="xml_retain">1</property>
                      </playlist>
                      <producer id="black" in="00:00:00.000" out="{LongTimeString(slides.TotalTime() - 1.5)}">
                        <property name="length">{LongTimeString(slides.TotalTime() - 1.5)}</property>
                        <property name="eof">pause</property>
                        <property name="resource">0</property>
                        <property name="aspect_ratio">1</property>
                        <property name="mlt_service">color</property>
                        <property name="mlt_image_format">rgba</property>
                        <property name="set.test_audio">0</property>
                      </producer>
                      <playlist id="background">
                        <entry producer="black" in="00:00:00.000" out="{LongTimeString(slides.TotalTime() - 1.5)}"/>
                      </playlist>
                    """;
            outputFile.WriteLine(preamble);

            foreach (Slide s in slides)
            {
                string fname = s.fileName.Replace("\\", "/");
                string rect_string = "";
                string format = "2";
                double dd;
                double scale;
                double x, y, w, h;
                double filter_out;
                string producer;

                scale = (double)frame_height / s.ImageHeight();

                if (slides.IndexOf(s) == 0)
                {
                    dd = 0;
                    filter_out = s.Duration() + 1.5;
                }
                else
                {
                    dd = 1.5;
                    filter_out = s.Duration() + 3.0;
                }

                foreach (Keyframe k in s.keys)
                {
                    dd += k.duration;
                    w = Math.Round((frame_width / 2) - (s.ImageWidth() * scale * k.zoomFactor * k.x), 3);
                    h = Math.Round((frame_height / 2) - (s.ImageHeight() * scale * k.zoomFactor * k.y), 3);
                    x = Math.Round(k.zoomFactor * s.ImageWidth() * scale, 3);
                    y = Math.Round(k.zoomFactor * s.ImageHeight() * scale, 3);
                    rect_string += $"{LongTimeString(dd)}$={w} {h} {x} {y} 1;";
                }
                if (s.IsLandscape())
                {
                    format = "1";
                }

                if (fname == "black")
                {
                    producer = $"""
                        <producer id="producer{producer_num++}" in="00:00:00.000" out="03:59:59.983">
                          <property name="length">04:00:00.000</property>
                          <property name="eof">pause</property>
                          <property name="resource">#FF000000</property>
                          <property name="aspect_ratio">1</property>
                          <property name="mlt_service">color</property>
                          <property name="mlt_image_format">rgba</property>
                          <property name="shotcut:caption">#FF000000</property>
                          <property name="shotcut:detail">#FF000000</property>
                          <property name="xml">was here</property>
                          <property name="seekable">1</property>
                        </producer>
                      """;
                }
                else if (fname == "white")
                {
                    producer = $"""
                        <producer id="producer{producer_num++}" in="00:00:00.000" out="03:59:59.983">
                          <property name="length">04:00:00.000</property>
                          <property name="eof">pause</property>
                          <property name="resource">#FFFFFFFF</property>
                          <property name="aspect_ratio">1</property>
                          <property name="mlt_service">color</property>
                          <property name="mlt_image_format">rgba</property>
                          <property name="shotcut:caption">#FFFFFFFF</property>
                          <property name="shotcut:detail">#FFFFFFFF</property>
                          <property name="xml">was here</property>
                          <property name="seekable">1</property>
                        </producer>
                      """;
                }
                else
                {
                    producer = $"""
                        <producer id="producer{producer_num++}" in="00:00:00.000" out="03:59:59.983">
                          <property name="length">04:00:00.000</property>
                          <property name="eof">pause</property>
                          <property name="resource">{fname}</property>
                          <property name="ttl">1</property>
                          <property name="aspect_ratio">1</property>
                          <property name="meta.media.progressive">1</property>
                          <property name="seekable">1</property>
                          <property name="format">{format}</property>
                          <property name="meta.media.width">{s.ImageWidth()}</property>
                          <property name="meta.media.height">{s.ImageHeight()}</property>
                          <property name="mlt_service">qimage</property>
                          <property name="shotcut:skipConvert">1</property>
                          <property name="shotcut:caption">{Path.GetFileName(fname)}</property>
                          <property name="meta.shotcut.vui">1</property>
                          <filter id="filter{filter_num++}" out="{LongTimeString(filter_out)}">
                            <property name="background">color:#00000000</property>
                            <property name="mlt_service">affine</property>
                            <property name="shotcut:filter">affineSizePosition</property>
                            <property name="transition.fix_rotate_x">0</property>
                            <property name="transition.fill">1</property>
                            <property name="transition.distort">0</property>
                            <property name="transition.rect">{rect_string}</property>
                            <property name="transition.valign">middle</property>
                            <property name="transition.halign">center</property>
                            <property name="shotcut:animIn">00:00:00.000</property>
                            <property name="shotcut:animOut">00:00:00.000</property>
                            <property name="transition.threads">0</property>
                          </filter>      
                          <filter id="filter{filter_num++}" out="{LongTimeString(filter_out)}">
                            <property name="rect">0 0 {frame_width} {frame_height} 1</property>
                            <property name="circle">0</property>
                            <property name="color">#ff000000</property>
                            <property name="radius">0</property>
                            <property name="mlt_service">qtcrop</property>
                            <property name="shotcut:filter">cropRectangle</property>
                          </filter>
                        </producer>
                      """;
                }
                outputFile.WriteLine(producer);

                if(producer_num > 1)
                {
                    string tractor = $"""
                          <tractor id="tractor{tractor_num++}" in="00:00:00.000" out="00:00:01.5">
                            <property name="shotcut:transition">lumaMix</property>
                            <track producer="producer{producer_num - 2}" in="{LongTimeString(previous_slide_duration)}" out="{LongTimeString(previous_slide_duration + 1.5)}"/>
                            <track producer="producer{producer_num - 1}" in="00:00:00.000" out="00:00:01.5"/>
                            <transition id="transition{transition_num++}" out="00:00:01.5">
                              <property name="a_track">0</property>
                              <property name="b_track">1</property>
                              <property name="factory">loader</property>
                              <property name="mlt_service">luma</property>
                              <property name="alpha_over">1</property>
                              <property name="fix_background_alpha">1</property>
                            </transition>
                            <transition id="transition{transition_num++}" out="00:00:01.5">
                              <property name="a_track">0</property>
                              <property name="b_track">1</property>
                              <property name="mlt_service">mix</property>
                              <property name="start">-1</property>
                              <property name="accepts_blanks">1</property>
                            </transition>
                          </tractor> 
                        """;
                    outputFile.WriteLine(tractor);
                }
                if (slides.IndexOf(s) == 0)
                {
                    previous_slide_duration = s.Duration();
                }
                else
                {
                    previous_slide_duration = s.Duration() + 1.5;
                }
            }

            string pl1 = $"""
                  <playlist id="playlist{playlist_num++}">
                    <property name="shotcut:video">1</property>
                    <property name="shotcut:name">Slides</property>
                    <entry producer="producer0" in="00:00:00.000" out="{LongTimeString(slides[0].Duration())}"/>
                    <entry producer="tractor0" in="00:00:00.000" out="00:00:01.5"/>
                """;
            outputFile.WriteLine(pl1);

            for (int s=1; s < slides.Count - 1; s++)
            {
                string pt = $"""
                          <entry producer="producer{s}" in="00:00:01.500" out="{LongTimeString(slides[s].Duration() + 1.5)}"/>
                          <entry producer="tractor{s}" in="00:00:00.000" out="00:00:01.5"/>
                      """;
                outputFile.WriteLine(pt);
            }

            string postamble = $"""
                  <entry producer="producer{slides.Count - 1}" in="00:00:01.500" out="{LongTimeString(slides[slides.Count - 1].Duration() + 1.5)}"/>
                </playlist>
                <playlist id="playlist{playlist_num++}">
                  <property name="shotcut:video">1</property>
                  <property name="shotcut:name">V1</property>
                  <blank length="00:00:00.017"/>
                </playlist>
                <playlist id="playlist{playlist_num++}">
                  <property name="shotcut:audio">1</property>
                  <property name="shotcut:name">Audio</property>
                  <blank length="00:00:00.017"/>
                </playlist>
                <tractor id="tractor{tractor_num++}" title="Shotcut version 25.03.29" in="00:00:00.000" out="{LongTimeString(slides.TotalTime() - 1.5)}">
                  <property name="shotcut">1</property>
                  <property name="shotcut:projectAudioChannels">2</property>
                  <property name="shotcut:projectFolder">0</property>
                  <property name="shotcut:skipConvert">0</property>
                  <property name="shotcut:scaleFactor">0.25</property>
                  <track producer="background"/>
                  <track producer="playlist0"/>
                  <track producer="playlist1"/>
                  <track producer="playlist2" hide="video"/>
                  <transition id="transition{transition_num++}">
                    <property name="a_track">0</property>
                    <property name="b_track">1</property>
                    <property name="mlt_service">mix</property>
                    <property name="always_active">1</property>
                    <property name="sum">1</property>
                  </transition>
                  <transition id="transition{transition_num++}">
                    <property name="a_track">0</property>
                    <property name="b_track">1</property>
                    <property name="version">0.1</property>
                    <property name="mlt_service">frei0r.cairoblend</property>
                    <property name="threads">0</property>
                    <property name="disable">1</property>
                  </transition>
                  <transition id="transition{transition_num++}">
                    <property name="a_track">0</property>
                    <property name="b_track">2</property>
                    <property name="mlt_service">mix</property>
                    <property name="always_active">1</property>
                    <property name="sum">1</property>
                  </transition>
                  <transition id="transition{transition_num++}">
                    <property name="a_track">1</property>
                    <property name="b_track">2</property>
                    <property name="version">0.1</property>
                    <property name="mlt_service">frei0r.cairoblend</property>
                    <property name="threads">0</property>
                    <property name="disable">0</property>
                  </transition>
                  <transition id="transition{transition_num++}">
                    <property name="a_track">0</property>
                    <property name="b_track">3</property>
                    <property name="mlt_service">mix</property>
                    <property name="always_active">1</property>
                    <property name="sum">1</property>
                  </transition>
                </tractor>
              </mlt>
              """;
            outputFile.WriteLine(postamble);

            return slides.Count;
        }
    }
}
