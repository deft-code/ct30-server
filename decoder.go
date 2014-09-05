package main

import (
	"bytes"
	"crypto/aes"
	"crypto/cipher"
	"encoding/hex"
	"flag"
	"fmt"
	"io/ioutil"
	"unicode"
)

var uuidFlag = flag.String("uuid", "001122334455", "uuid from /cloud")
var authkeyFlag = flag.String("authkey", "00112233", "authkey from /cloud")

func clean(data []byte) string {
	fields := bytes.FieldsFunc(data, func(r rune) bool {
		return !(unicode.IsDigit(r) || unicode.IsLetter(r) || unicode.IsPunct(r))
	})
	cleanData := bytes.Join(fields, []byte("?"))
	return string(cleanData)
}

func work() error {

	var err error
	//block, err := aes.NewCipher([]byte(*authkeyFlag + *authkeyFlag))
	//block, err := aes.NewCipher([]byte("00000000" + *authkeyFlag))

	//key := []byte(*authkeyFlag + "00000000")
	//key, err := hex.DecodeString("00000000" + "00000000" + "00000000" + *authkeyFlag)
	key, err := hex.DecodeString(*authkeyFlag + "00000000" + "00000000" + "00000000")

	if err != nil {
		return err
	}
	fmt.Println("key:", hex.EncodeToString(key))
	block, err := aes.NewCipher(key)
	if err != nil {
		return err
	}

	csvBytes, err := ioutil.ReadFile("ct30.log.csv")
	if err != nil {
		return err
	}

	lines := bytes.Split(csvBytes, []byte("\n"))

	for _, line := range lines {
		values := bytes.Split(line, []byte(","))
		fmt.Println(string(values[0]))

		eiv := make([]byte, hex.DecodedLen(len(values[2])))
		_, err = hex.Decode(eiv, values[2])
		if err != nil {
			return err
		}
		fmt.Printf("EIV[%d]: %s\n", len(eiv), string(values[2]))
		cbc := cipher.NewCBCDecrypter(block, eiv)

		req := make([]byte, hex.DecodedLen(len(values[1])))
		_, err = hex.Decode(req, values[1])
		if err != nil {
			return err
		}
		cbc.CryptBlocks(req, req)
		fmt.Println(len(req), clean(req))

		res := make([]byte, hex.DecodedLen(len(values[3])))
		_, err = hex.Decode(res, values[3])
		if err != nil {
			return err
		}
		fmt.Println(len(res), clean(res))
		fmt.Println()
	}
	return nil
}

func main() {
	flag.Parse()
	err := work()
	if err != nil {
		fmt.Println(err)
	}
}
