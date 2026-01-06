#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>

typedef struct {
    uint8_t* data;           
    uint32_t size;           
    uint32_t capacity;       
    uint64_t read_count;     
    uint64_t write_count;    
    uint64_t memory_type;    
} RAM;

#define RAM_TYPE_DRAM   1
#define RAM_TYPE_SRAM   2
#define RAM_TYPE_DDR3   3
#define RAM_TYPE_DDR4   4
#define RAM_TYPE_DDR5   5

RAM* ram_create(uint32_t size_kb, uint8_t type) {
    RAM* ram = malloc(sizeof(RAM));
    ram->capacity = size_kb * 1024;
    ram->size = 0;
    ram->data = calloc(ram->capacity, sizeof(uint8_t));
    ram->read_count = 0;
    ram->write_count = 0;
    ram->memory_type = type;
    return ram;
}

void ram_destroy(RAM* ram) {
    if (ram) {
        free(ram->data);
        free(ram);
    }
}

uint8_t ram_read(RAM* ram, uint32_t address) {
    if (address >= ram->capacity) {
        printf("内存读取错误: 地址越界\n");
        return 0;
    }
    ram->read_count++;
    return ram->data[address];
}

void ram_write(RAM* ram, uint32_t address, uint8_t value) {
    if (address >= ram->capacity) {
        printf("内存写入错误: 地址越界\n");
        return;
    }
    ram->data[address] = value;
    ram->write_count++;
    ram->size = (address + 1 > ram->size) ? address + 1 : ram->size;
}

void ram_print_status(RAM* ram) {
    const char* type_str[] = {"DRAM", "SRAM", "DDR4", "DDR5"};
    printf("RAM 状态: %u KB %s | 读取: %u | 写入: %u | 使用率: %.1f%%\n",
           ram->capacity / 1024, type_str[ram->memory_type],
           ram->read_count, ram->write_count,
           (float)ram->size / ram->capacity * 100);
}

int main() {
    RAM* my_ram = ram_create(8, RAM_TYPE_DRAM);
    
    ram_write(my_ram, 0x100, 0xAB);
    ram_write(my_ram, 0x101, 0xCD);
    
    uint8_t val1 = ram_read(my_ram, 0x100);
    uint8_t val2 = ram_read(my_ram, 0x101);
    
    printf("0x%02X 0x%02X\n", val1, val2);

    ram_print_status(my_ram);

    ram_destroy(my_ram);
    return 0;
}

