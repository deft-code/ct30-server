package main

import (
	"code.google.com/p/go.crypto/pbkdf2"

	"bytes"
	"crypto/aes"
	"crypto/cipher"
	"crypto/sha1"
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

type attempt struct {
	pass, salt []byte
}

func (a attempt) work() error {
	fmt.Printf("password[%d]: %s, %s\n", len(a.pass), hex.EncodeToString(a.pass), clean(a.pass))
	fmt.Printf("salt[%d]: %s, %s\n", len(a.salt), hex.EncodeToString(a.salt), clean(a.salt))

	key := pbkdf2.Key(a.pass, a.salt, 1000, 16, sha1.New)
	fmt.Printf("key[%d]: %s, %s\n", len(key), hex.EncodeToString(key), clean(key))

	const request_hex = "15d1069b39845c4a811ec4a9aa03a003d42ebda3af67e8a74145a6eb1714f7d332cf9fce3ddbd040c97e1578797507a5c01644efacabbab9934dd040699d0855d423590af7e4dae035ad6603511bd17f2bc390aac5db344056eef77548c7f09972af92ec9678e3c0eb8b7cae69009263bd7ff6d1603d1f53a4f5db91a1803b4c02b996a2fcf3ad5f5e57280e913acda55a7b645325a93dc47f41c9803ca2f4f3fc5e682d39958a979c36d8f4289c9b58d196c569d1ee720504a52f7ac8f59bf5575c3b6ee8a7f6aa83d8415ba51838b8c8a0c377ed3cd1987014ace4c18ae35cfd565cba6fc8676658de3cd56fd945df8963b3b562417c8cd899289b635a4cc5b08c2bd74f55cf8d4affb94a0ea5c6684c96e3cb4db509fa70438768587dae6c3fcfdea98dc462ac73ad9590861aa84f"
	const eiv_hex = "8d979c43e36cbe7ada9f8e0463b5feba"
	const response_hex = "051b90781213877deac719ba7e3d44a55664cb88c27e649f82dc8ef4fe206e0a"

  var err error

	request := make([]byte, hex.DecodedLen(len(request_hex)))
	_, err = hex.Decode(request, []byte(request_hex))
	if err != nil {
		return err
	}

	eiv := make([]byte, hex.DecodedLen(len(eiv_hex)))
	_, err = hex.Decode(eiv, []byte(eiv_hex))
	if err != nil {
		return err
	}

	response := make([]byte, hex.DecodedLen(len(response_hex)))
	_, err = hex.Decode(response, []byte(response_hex))
	if err != nil {
		return err
	}

	block, err := aes.NewCipher(key)
	if err != nil {
		return err
	}

	cbc := cipher.NewCBCDecrypter(block, eiv)

	cbc.CryptBlocks(request, request)
	fmt.Printf("request[%d]:\n%s\n", len(request), clean(request))

	fmt.Printf("response[%d]:\n%s\n", len(response), clean(response))
	fmt.Println()

	return nil
}

func work() error {
	var err error

	authkey_hex, err := hex.DecodeString(*authkeyFlag)
	if err != nil {
		panic(err)
	}

	uuid_hex, err := hex.DecodeString(*uuidFlag)
	if err != nil {
		panic(err)
	}

	authkey_ascii := []byte(*authkeyFlag)
	uuid_ascii := []byte(*uuidFlag)
	nozipbpf_ascii := []byte("NOZIPBPF")

	_ = authkey_ascii
	_ = authkey_hex
	_ = nozipbpf_ascii
	_ = uuid_ascii
	_ = uuid_hex

	//key := pbkdf2.Key(authkey_hex, uuid_hex, 1000, 16, sha1.New)
	//key := pbkdf2.Key(authkey_ascii, uuid_hex, 1000, 16, sha1.New)
	//key := pbkdf2.Key(authkey_ascii, uuid_ascii, 1000, 16, sha1.New)
	//key := pbkdf2.Key(authkey_hex, uuid_ascii, 1000, 16, sha1.New)
	//key := pbkdf2.Key(authkey_hex, nozipbpf_ascii, 1000, 16, sha1.New)

	//key := pbkdf2.Key(nozipbpf_ascii, uuid_ascii, 1000, 16, sha1.New)
	//key := pbkdf2.Key(nozipbpf_ascii, uuid_hex, 1000, 16, sha1.New)
	key := pbkdf2.Key(nozipbpf_ascii, authkey_hex, 1000, 16, sha1.New)
	//key := pbkdf2.Key(nozipbpf_ascii, authkey_ascii, 1000, 16, sha1.New)

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

	authkey_hex, err := hex.DecodeString(*authkeyFlag)
	if err != nil {
		panic(err)
	}

	uuid_hex, err := hex.DecodeString(*uuidFlag)
	if err != nil {
		panic(err)
	}

	authkey_ascii := []byte(*authkeyFlag)
	uuid_ascii := []byte(*uuidFlag)
	nozipbpf_ascii := []byte("NOZIPBPF")

	_ = authkey_ascii
	_ = authkey_hex
	_ = nozipbpf_ascii
	_ = uuid_ascii
	_ = uuid_hex

	var attempts = []attempt{
		{authkey_ascii, nozipbpf_ascii},
		{authkey_ascii, uuid_ascii},
		{authkey_ascii, uuid_hex},
		{authkey_hex, nozipbpf_ascii},
		{authkey_hex, uuid_ascii},
		{authkey_hex, uuid_hex},

		{nozipbpf_ascii, authkey_ascii},
		{nozipbpf_ascii, authkey_hex},
		{nozipbpf_ascii, uuid_ascii},
		{nozipbpf_ascii, uuid_hex},

		{uuid_ascii, authkey_ascii},
		{uuid_ascii, authkey_hex},
		{uuid_ascii, nozipbpf_ascii},
		{uuid_hex, authkey_ascii},
		{uuid_hex, authkey_hex},
		{uuid_hex, nozipbpf_ascii},
	}

	for _, a := range attempts {
		err := a.work()
		if err != nil {
			fmt.Println(err)
		}
	}
}
