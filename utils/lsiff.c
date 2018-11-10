
#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

#include <sys/types.h>
#include <sys/mman.h> /* prolly POSIX-only. write a patch if you want it to */
#include <sys/stat.h> /* work on your OS */
#include <unistd.h>
#include <fcntl.h>

struct iffhdr {
    uint32_t magi;
    uint32_t size;
};

inline static void writehdr(int n, int tabs, const struct iffhdr* hdr,
        const void* begin) {
    for (int i = 0; i < tabs; ++i) printf("\t");

    char mags[5];
    for (int i = 0; i < 4; ++i) {
        mags[i] = (hdr->magi >> (i*8))& 0xFF;
    }
    mags[4] = 0;
    printf("[%02i] 0x%08zX: %s size=0x%08X\n", n, (size_t)hdr-(size_t)begin,
            mags, hdr->size);
}

int main(int argc, char* argv[]) {
    if (argc < 2) {
        printf("No filename given\n");
        return 1;
    }

    int f = open(argv[1], O_RDONLY);
    if (f < 0) {
        printf("Can't open file.\n");
        return 1;
    }
    struct stat st;
    if (fstat(f, &st) < 0) {
        printf("Can't stat file.\n");
        close(f);
        return 1;
    }

    void* d = mmap(NULL, (size_t)st.st_size, PROT_READ, MAP_PRIVATE, f, 0);
    if (!d || d == MAP_FAILED) {
        printf("Can't mmap file.\n");
        close(f);
        return 1;
    }

    const struct iffhdr* form = (const struct iffhdr*)d;
    writehdr(0, 0, form, d);

    int i;
    for (const struct iffhdr *cur = form + 1,
                             *end = (const struct iffhdr*)((const uint8_t*)d
                                 + (size_t)st.st_size);
                cur < end;
                cur = (const struct iffhdr*)((const uint8_t*)cur + cur->size
                    + sizeof(struct iffhdr)), ++i) {
        writehdr(i, 1, cur, d);
    }

    munmap(d, (size_t)st.st_size);
    close(f);
    return 0;
}

