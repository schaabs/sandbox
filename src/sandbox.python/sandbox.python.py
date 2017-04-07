import hashlib
import shutil
import io
import gzip
import platform
import os
import struct
import json
import mmap

class ElfConst:
    CLASS_32 = 1
    CLASS_64 = 2

    DATA_LE = 1
    DATA_BE = 2

    TYPE_RELOC = 1
    TYPE_EXEC = 2
    TYPE_SHARED = 3
    TYPE_CORE = 4

class Layout:
    ElfIdent = b'=4sBBBBBxxxxxxx'        

    ElfFileHeader32BE = b'>HHIIIIIHHHHHH'
    
    ElfFileHeader32LE = b'<HHIIIIIHHHHHH'
                                                        
    ElfFileHeader64BE = b'>HHIQQQIHHHHHH'
    
    ElfFileHeader64LE = b'<HHIQQQIHHHHHH'

    ElfProgramHeader32BE = b'>IIIIIIII'
    ElfProgramHeader32LE = b'<IIIIIIII'
    ElfProgramHeader64BE = b'>IIQQQQQQ'
    ElfProgramHeader64LE = b'<IIQQQQQQ'

    ElfNoteHeader32BE = b'>III'
    ElfNoteHeader32LE = b'<III'
    ElfNoteHeader64BE = b'>III'
    ElfNoteHeader64LE = b'<III'

class ExplicitLayout:
    layout = None
    
    def size(self):
        if not self.layout:
            return 0
        return struct.calcsize(self.layout)

    def _struct_unpack_from(self, file, offset=0): 
        file.seek(offset)
        bytestr = file.read(self.size());
        return struct.unpack(self.layout, bytestr)

class ElfIdent(ExplicitLayout):                                                 

    magic = None

    elfClass = None

    elfData = None

    fileVersion = None

    fileAbi = None
    
    abiVersion = None

    
    def __init__(self):

        self.layout = Layout.ElfIdent

    def unpack_from(self, file, offset=0):   
        print 'offset=' + hex(offset) + ' size=' + hex(self.size())
        (self.magic, self.elfClass, self.elfData, self.fileVersion, self.fileAbi, self.abiVersion) = self._struct_unpack_from(file, offset)
        print self

    def is_valid(self):

        #if the magic string doesn't match the expected '\x7fELF' return false
        return self.magic == '\x7fELF'

    
    def __str__(self):
        dict = {
                    'magic': self.magic,
                    'elfClass': hex(self.elfClass),
                    'elfData': hex(self.elfData),
                    'fileVersion': hex(self.fileVersion),
                    'fileAbi': hex(self.fileAbi),
                    'abiVersion': hex(self.abiVersion)
               }

        return json.dumps(dict)

      

class ElfFileHeader(ExplicitLayout):
                     
    type = None
    machine = None
    version = None
    entry = None
    phoff = None
    shoff = None
    flags = None
    ehsize = None
    phentsize = None
    phnum = None
    shentsize = None
    shnum = None
    shstrndx = None

    def __init__(self, elfident):

        if elfident.elfClass == ElfConst.CLASS_32:
            if elfident.elfData == ElfConst.DATA_BE:
                self.layout = Layout.ElfFileHeader32BE
            elif elfident.elfData == ElfConst.DATA_LE:   
                self.layout = Layout.ElfFileHeader32LE
        elif elfident.elfClass == ElfConst.CLASS_64:     
            if elfident.elfData == ElfConst.DATA_BE:
                self.layout = Layout.ElfFileHeader64BE
            elif elfident.elfData == ElfConst.DATA_LE:   
                self.layout = Layout.ElfFileHeader64LE

    # returns the data at the specified offset as an ElfFileHeader
    def unpack_from(self, file, offset):                 
        print 'offset=' + hex(offset) + ' size=' + hex(self.size())
        (self.type, self.machine, self.version, self.entry,
         self.phoff, self.shoff, self.flags, self.ehsize,
         self.phentsize, self.phnum, self.shentsize, self.shnum,
         self.shstrndx) = self._struct_unpack_from(file, offset)
        print self

    def __str__(self):
        dict = {
                    'type': hex(self.type), 
                    'machine': hex(self.machine), 
                    'version': hex(self.version), 
                    'entry': hex(self.entry),
                    'phoff': hex(self.phoff),
                    'shoff': hex(self.shoff),
                    'flags': hex(self.flags),
                    'ehsize': hex(self.ehsize),
                    'phentsize': hex(self.phentsize),
                    'phnum': hex(self.phnum),
                    'shentsize': hex(self.shentsize),
                    'shnum': hex(self.shnum),
                    'shstrndx': hex(self.shstrndx)
               }

        return json.dumps(dict)   
        
        
class ElfProgramHeader(ExplicitLayout):
    type = None
    offset = None
    vaddr = None
    paddr = None
    filesz = None
    memsz = None
    flags = None
    align = None
     
    def __init__(self, elfident):

        self._elfident = elfident
        if elfident.elfClass == ElfConst.CLASS_32:
            if elfident.elfData == ElfConst.DATA_BE:
                self.layout = Layout.ElfProgramHeader32BE
            elif elfident.elfData == ElfConst.DATA_LE:   
                self.layout = Layout.ElfProgramHeader32LE
        elif elfident.elfClass == ElfConst.CLASS_64:     
            if elfident.elfData == ElfConst.DATA_BE:
                self.layout = Layout.ElfProgramHeader64BE
            elif elfident.elfData == ElfConst.DATA_LE:   
                self.layout = Layout.ElfProgramHeader64LE 

    def unpack_from(self, file, offset):
        print 'offset=' + hex(offset) + ' size=' + hex(self.size())
        if self._elfident.elfClass == ElfConst.CLASS_32:
            (self.type, self.offset, self.vaddr, self.paddr,
             self.filesz, self.memsz, self.flags, self.align) = self._struct_unpack_from(file, offset)
        else:
            (self.type, self.flags, self.offset, self.vaddr,
             self.paddr, self.filesz, self.memsz, self.align) = self._struct_unpack_from(file, offset)
        print self
   
    def __str__(self):
       str = ''.join([ 
                        '(type=', hex(self.type),
                        ' offset=', hex(self.offset),
                        ' vaddr=', hex(self.vaddr),
                        ' paddr=', hex(self.paddr),
                        ' filesz=', hex(self.filesz),
                        ' memsz=', hex(self.memsz),
                        ' flags=', hex(self.flags),
                        ' align', hex(self.align), ')'
                     ])
       return str

class ElfNote:
    noteHeader = None
    name = None
    descr = None

class ElfNoteHeader(ExplicitLayout):
    namesz = None
    descsz = None
    type = None

    def __init__(self, elfident):

        if elfident.elfClass == ElfConst.CLASS_32:
            if elfident.elfData == ElfConst.DATA_BE:
                self.layout = Layout.ElfNoteHeader32BE
            elif elfident.elfData == ElfConst.DATA_LE:   
                self.layout = Layout.ElfNoteHeader32LE
        elif elfident.elfClass == ElfConst.CLASS_64:     
            if elfident.elfData == ElfConst.DATA_BE:
                self.layout = Layout.ElfNoteHeader64BE
            elif elfident.elfData == ElfConst.DATA_LE:   
                self.layout = Layout.ElfNoteHeader64LE 

    def unpack_from(self, file, offset):
        (self.namesz, self.descsz, self.type) = self._struct_unpack_from(file, offset)
   
    def __str__(self):
        dict = {
                    'namesz': hex(self.namesz), 
                    'descsz': hex(self.descsz), 
                    'type': hex(self.type)
               }

        return json.dumps(dict)  

class ElfFile:

    ident = None
    fileHeader = None
    programHeaders = [ ]
    notes = [ ]

    @staticmethod
    def unpack_from(file, offset=0):
        elffile = ElfFile()

        elffile.ident = ElfIdent()

        elffile.ident.unpack_from(file, offset)

        if not elffile.ident.is_valid():
            return None

        elffile.fileHeader = ElfFileHeader(elffile.ident)

        elffile.fileHeader.unpack_from(file, offset + elffile.ident.size())

        elffile._unpack_program_headers(file, offset)

        return elffile
        
    def _unpack_program_headers(self, file, offset):
        for i in range(0, self.fileHeader.phnum):
            ph = ElfProgramHeader(self.ident)
            
            print offset + self.fileHeader.phoff + (i * self.fileHeader.phentsize)
            ph.unpack_from(file, offset + self.fileHeader.phoff + (i * self.fileHeader.phentsize))
            
            self.programHeaders.append(ph)

            if ph.type == 4:
                _unpack_notes(self, file, ph.offset, ph.offset + ph.filesz)

    def __str__(self):
        filestr = 'ident:\n' + str(self.ident) + '\nfileHeader:\n' + str(self.fileHeader) + '\nprogramHeaders:\n' + '\n'.join(str(ph) for ph in self.programHeaders)

        return filestr

if __name__ == '__main__':
    with open('libcoreclr.so', 'rb') as corefile:   
        print ''
        print ''
        print(ElfFile.unpack_from(corefile, 0))

        
        
        
     
