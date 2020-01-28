/*
Copyright 2015 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A local copy of the license and additional notices are located with the
source distribution at:

http://github.com/Esri/lerc/

Contributors:  Thomas Maurer
*/

#ifndef CNTZIMAGE_H
#define CNTZIMAGE_H

#include <vector>
//#include "TImage.hpp"

#include <cstring>
#include <stdlib.h>
#include "Image.h"

NAMESPACE_LERC_START

class CntZ
{
public:
  float cnt, z;
  bool operator == (const CntZ& cz) const    { return cnt == cz.cnt && z == cz.z; }
  bool operator != (const CntZ& cz) const    { return cnt != cz.cnt || z != cz.z; }
  void operator += (const CntZ& cz)          { cnt += cz.cnt;  z += cz.z; }
};

template< class Element >
class TImage : public Image
{
public:
  TImage() : data_(0)  {}
  TImage(const TImage& tImg) : data_(0)  { type_ = tImg.type_;  *this = tImg; }
  virtual ~TImage()                      { clear(); }

  /// assignment
  virtual TImage& operator=(const TImage& tImg);

  bool resize(int width, int height);
  virtual void clear();

  /// get data
  Element getPixel(int row, int col) const            { return data_[row * width_ + col]; }
  const Element& operator() (int row, int col) const  { return data_[row * width_ + col]; }
  const Element* getData() const                      { return data_; }

  /// set data
  void setPixel(int row, int col, Element element)    { data_[row * width_ + col] = element; }
  Element& operator() (int row, int col)              { return data_[row * width_ + col]; }
  Element* getData()                                  { return data_; }

  /// compare
  bool operator == (const Image& img) const;
  bool operator != (const Image& img) const           { return !operator==(img); };

protected:
  Element* data_;
};

// -------------------------------------------------------------------------- ;

template< class Element >
bool TImage< Element >::resize(int width, int height)
{
  if (width <= 0 || height <= 0)
    return false;

  if (width == width_ && height == height_ && data_)
    return true;

  free(data_);
  width_ = 0;
  height_ = 0;

  data_ = (Element*)malloc(width * height * sizeof(Element));
  if (!data_)
    return false;

  width_ = width;
  height_ = height;

  return true;
}

// -------------------------------------------------------------------------- ;

template< class Element >
void TImage< Element >::clear()
{
  free(data_);
  data_ = 0;
  width_ = 0;
  height_ = 0;
}

// -------------------------------------------------------------------------- ;

template< class Element >
TImage< Element >& TImage< Element >::operator = (const TImage& tImg)
{
  // allow copying image to itself
  if (this == &tImg) return *this;

  // only for images of the same type!
  // conversions are implemented in the derived classes

  if (!resize(tImg.getWidth(), tImg.getHeight()))
    return *this;    // return empty image if resize fails

  if (data_ && tImg.data_)
  {
    memcpy(data_, tImg.data_, getSize() * sizeof(Element));
    Image::operator=(tImg);
  }

  return *this;
}

// -------------------------------------------------------------------------- ;

template< class Element >
bool TImage< Element >::operator == (const Image& img) const
{
  if (!Image::operator == (img)) return false;

  const Element* ptr0 = getData();
  const Element* ptr1 = ((const TImage&)img).getData();
  int cnt = getSize();
  while (cnt--)
    if (*ptr0++ != *ptr1++)
      return false;

  return true;
}

//*********************************************************************************************
//*********************************************************************************************

/**	count / z image
 *
 *	count can also be a weight, therefore float;
 *	z can be elevation or intensity;
 */

class CntZImage : public TImage< CntZ >
{
public:
  CntZImage();
  virtual ~CntZImage()  {};
  std::string getTypeString() const  { return "CntZImage "; }

  bool resizeFill0(int width, int height);

  static unsigned int computeNumBytesNeededToReadHeader();

  /// read succeeds only if maxZError on file <= maxZError requested
  bool read(Byte** ppByte, double maxZError, bool onlyHeader = false, bool onlyZPart = false);

protected:

  struct InfoFromComputeNumBytes
  {
    double maxZError;
    bool cntsNoInt;
    int numTilesVertCnt;
    int numTilesHoriCnt;
    int numBytesCnt;
    float maxCntInImg;
    int numTilesVertZ;
    int numTilesHoriZ;
    int numBytesZ;
    float maxZInImg;
  };

  bool readTiles(bool zPart, double maxZErrorInFile, int numTilesVert, int numTilesHori, float maxValInImg, Byte* bArr);

  bool readCntTile(Byte** ppByte, int i0, int i1, int j0, int j1);
  bool readZTile(Byte** ppByte, int i0, int i1, int j0, int j1, double maxZErrorInFile, float maxZInImg);

  static int numBytesFlt(float z);    // returns 1, 2, or 4
  static bool readFlt(Byte** ppByte, float& z, int numBytes);

protected:

  InfoFromComputeNumBytes    m_infoFromComputeNumBytes;
  std::vector<unsigned int>  m_tmpDataVec;             // used in read fcts
  bool                       m_bDecoderCanIgnoreMask;  // "
};

NAMESPACE_LERC_END
#endif
