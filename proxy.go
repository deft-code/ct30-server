package main

import (
	"bytes"
<<<<<<< HEAD
	"encoding/hex"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
=======
	"compress/gzip"
	"encoding/hex"
	"flag"
	"io/ioutil"
>>>>>>> 345c7f45cd3589595b5e1fe63b0f6dc6f6e0ae2b
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
<<<<<<< HEAD
	"strings"
=======
>>>>>>> 345c7f45cd3589595b5e1fe63b0f6dc6f6e0ae2b
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
<<<<<<< HEAD
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
=======
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
>>>>>>> 345c7f45cd3589595b5e1fe63b0f6dc6f6e0ae2b
}

func handler(rw http.ResponseWriter, req *http.Request) {

<<<<<<< HEAD
	err := req_printer(req)
	if err != nil {
		log.Print(err)
	}

=======
	dump, err := httputil.DumpRequest(req, true)

	if err != nil {
		log.Fatalf("failed read request: %s", err)
	}

	log.Println("request:", string(dump))

>>>>>>> 345c7f45cd3589595b5e1fe63b0f6dc6f6e0ae2b
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
<<<<<<< HEAD
	log.Println("Start serving on port", *port)
=======
	log.Println("Start serving on port 8000")
>>>>>>> 345c7f45cd3589595b5e1fe63b0f6dc6f6e0ae2b
	http.ListenAndServe(*port, nil)
	os.Exit(0)
}
