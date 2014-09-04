package main

import (
	"bytes"
	"compress/gzip"
	"encoding/hex"
	"flag"
	"io/ioutil"
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
)

var urlFlag = flag.String("radio_url", "http://ws.radiothermostat.com", "Don't include the path component")
var port = flag.String("port", ":8080", "The port the proxy should listen on.")

var reverseProxy *httputil.ReverseProxy

type LoggedResponseWriter struct {
	http.ResponseWriter
}

func (w LoggedResponseWriter) WriteHeader(status int) {
	log.Println("responded:", status, w.Header())
	w.ResponseWriter.WriteHeader(status)
}

func (w LoggedResponseWriter) Write(msg []byte) (int, error) {
	log.Println("hex response:", hex.EncodeToString(msg))

	buf := bytes.NewBuffer(msg)
	r, err := gzip.NewReader(buf)
	if err != nil {
		log.Println("Unable to create gzip:", err)
	} else {
		plain_msg, err := ioutil.ReadAll(r)
		if err != nil {
			log.Println("Unable to read compressed:", err)
		} else {
			log.Println("gzip response:", string(plain_msg))
		}
	}

	log.Println("response:", string(msg))
	return w.ResponseWriter.Write(msg)
}

func handler(rw http.ResponseWriter, req *http.Request) {

	dump, err := httputil.DumpRequest(req, true)

	if err != nil {
		log.Fatalf("failed read request: %s", err)
	}

	log.Println("request:", string(dump))

	w := LoggedResponseWriter{rw}
	reverseProxy.ServeHTTP(w, req)
}

func main() {

	url, err := url.ParseRequestURI(*urlFlag)
	if err != nil {
		log.Fatalf("failed to parse url '%s': %s", *urlFlag, err)
	}
	reverseProxy = httputil.NewSingleHostReverseProxy(url)

	http.HandleFunc("/", handler)
	log.Println("Start serving on port 8000")
	http.ListenAndServe(*port, nil)
	os.Exit(0)
}
