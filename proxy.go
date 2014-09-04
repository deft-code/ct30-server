package main

import (
	"bytes"
	"encoding/hex"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
	"strings"
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
	log.Printf("response[%d]: %s\n", len(msg), hex.EncodeToString(msg))
	return w.ResponseWriter.Write(msg)
}

type CT_post struct {
	Uuid      string `json:"uuid"`
	FormatVer string `json:"format_ver"`
	EIV       string `json:"eiv"`
}

type Chain struct {
	Cause error
	msg   string
}

func (c *Chain) Error() string {
	return fmt.Sprintf("%s caused by %q", c.msg, c.Cause)
}

func NewChain(msg string, cause error) *Chain {
	return &Chain{cause, msg}
}

func req_printer(req *http.Request) error {
	dump, err := httputil.DumpRequest(req, true)

	if err != nil {
		return NewChain("unable to read request", err)
	}

	i := bytes.IndexByte(dump, byte('}')) + 1
	if i <= 0 {
		return errors.New("unable to find end of json")
	}

	english := string(dump[:i])
	payload := dump[i:]
	log.Println("request:\n", english)
	log.Printf("Payload[%d]: %s\n", len(payload), hex.EncodeToString(payload))

	j := strings.Index(english, "{")
	if j < 0 {
		return errors.New("unable to find start of json")
	}

	json_payload := dump[j:i]

	post := &CT_post{}
	err = json.Unmarshal(json_payload, post)
	if err != nil {
		return NewChain("bad json", err)
	}

	log.Printf("json payload: %s", string(json_payload))
	log.Printf("EIV: %q", post.EIV)
	return nil
}

func handler(rw http.ResponseWriter, req *http.Request) {

	err := req_printer(req)
	if err != nil {
		log.Print(err)
	}

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
	log.Println("Start serving on port", *port)
	http.ListenAndServe(*port, nil)
	os.Exit(0)
}
